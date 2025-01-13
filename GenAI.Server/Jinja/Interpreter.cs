using System.Collections;
using System.Linq.Expressions;
using System.Linq;
using System.Text;
using GenAI.Server.Jinja.Expressions;
using GenAI.Server.Jinja.Runtime;
using BinaryExpression = GenAI.Server.Jinja.Expressions.BinaryExpression;
using MemberExpression = GenAI.Server.Jinja.Expressions.MemberExpression;
using UnaryExpression = GenAI.Server.Jinja.Expressions.UnaryExpression;

namespace GenAI.Server.Jinja
{
    public class Interpreter
    {
        private readonly Environment _environment;

        public Interpreter(Environment environment)
        {
            _environment = environment;
        }

        public RuntimeValue Run(ProgramNode program)
        {
            return EvaluateBlock(program.Body, _environment);
        }

        private RuntimeValue EvaluateBlock(List<Statement> statements, Environment env)
        {
            var result = new StringBuilder();
            foreach (var statement in statements)
            {
                var evaluated = Evaluate(statement, env);
                if (evaluated != null && evaluated is StringValue strVal)
                {
                    result.Append(strVal.Value);
                }
            }
            return new StringValue(result.ToString());
        }

        private RuntimeValue Evaluate(Statement statement, Environment env)
        {
            switch (statement)
            {
                case CallExpression callExpression:
                    return EvaluateCallExpression(callExpression, env);
                case MemberExpression memberExpression:
                    return EvaluateMemberExpression(memberExpression, env);
                case ArrayLiteral arrayLiteral:
                    { 
                        List<RuntimeValue> values = new();
                        foreach (var item in (IEnumerable)arrayLiteral.Value)
                        {
                            //values.Add(EvaluateLiteral(item, env));
                        }
                        return new ArrayValue(values.ToArray());
                    }
                    break;
                case LiteralExpression literal:
                    return EvaluateLiteral(literal, env);
                case IfStatement ifStatement:
                    return EvaluateIf(ifStatement, env);
                case SetStatement setStatement:
                    return EvaluateSet(setStatement, env);
                case ForStatement forStatement:
                    return EvaluateFor(forStatement, env);
                case BinaryExpression binaryExpression:
                    return EvaluateBinaryExpression(binaryExpression, env);
                case IdentifierExpression identifierExpression:
                    return env.LookupVariable(identifierExpression.Value);
                case TestExpression testExpression:
                    return EvaluateTestExpression(testExpression, env);
                case UnaryExpression unaryExpression:
                    return EvaluateUnaryExpression(unaryExpression, env);
                case FilterExpression filterExpression:
                    return EvaluateFilterExpression(filterExpression, env);
                case KeywordArgumentExpression keywordArgumentExpression:
                    return new ArgumentValue(keywordArgumentExpression.Key.Value, Evaluate(keywordArgumentExpression.Value, env));
                case MacroStatement macroStatement:
                    {
                        EvaluateMacroStatement(macroStatement, env);
                        return new NullValue();
                    }
                    break;
                default:
                    throw new Exception($"Unknown statement type: {statement.GetType().Name} [{statement.ToCode()}]");
            }
        }

        private void EvaluateMacroStatement(MacroStatement macroStatement, Environment env)
        {
            var name = ((IdentifierExpression)macroStatement.Name).Value;
            var args = macroStatement.Args.Select(arg => ((IdentifierExpression)arg).Value).ToList();

            env.Set(name, new FunctionValue((v, e) =>
            {
                //var macroEnv = e.CreateChildEnvironment();
                //foreach (var vv in env.Variables)
                //{
                //    macroEnv.Set(vv.Key, vv.Value);
                //}

                //for (int i = 0; i < args.Count; i++)
                //{
                //    macroEnv.Set(args[i], v[i]);
                //}
                return EvaluateBlock(macroStatement.Body, env);
            }));

        }

        private RuntimeValue EvaluateFilterExpression(FilterExpression node, Environment env)
        {
            // First, evaluate the base expression (e.g., `name`)
            var baseValue = Evaluate(node.BaseExpression, env);

            // Get the filter function from the environment (e.g., `upper`, `replace`)
            var filterFunction = env.LookupVariable(node.FilterName.Value);
            if (!(filterFunction is FunctionValue function))
            {
                throw new Exception($"Unknown filter: {node.FilterName.Value} evaluating {node.ToCode()}");
            }

            // Evaluate the arguments to the filter
            var args = node.Arguments.Select(arg => Evaluate(arg, env)).ToList();

            // Apply the filter function to the base value
            return function.Call(new List<RuntimeValue> { baseValue }.Concat(args).ToList(), env);
        }

        private RuntimeValue EvaluateUnaryExpression(UnaryExpression unaryExpression, Environment env)
        {
            var operand = Evaluate(unaryExpression.Argument, env);
            switch (unaryExpression.Operator)
            {
                case "-":
                    if (operand is NumericValue numericValue)
                    {
                        return new NumericValue(-(int)numericValue.Value);
                    }
                    break;
                case "not":
                    {
                        RuntimeValue value = operand;

                        if (!(value is BooleanValue booleanValue))
                            value = this.CoerceBooleanValue(operand);
                        {
                            return new BooleanValue(!(bool)value.Value);
                        }
                    }
                    
                    break;
            }
            throw new Exception($"Unknown unary operator: {unaryExpression.ToCode()}");
        }

        private RuntimeValue EvaluateTestExpression(TestExpression node, Environment env)
        {
            // Evaluate the left-hand side (e.g., `system_message`)
            var operand = Evaluate(node.Operand, env);

            // Handle the `defined` test
            if (node.Test.Value == "defined")
            {
                // Check if the operand is defined (i.e., not UndefinedValue)
                bool isDefined = !(operand is UndefinedValue);
                return new BooleanValue(node.Negate ? !isDefined : isDefined);  // Handle negation if necessary
            }
            else if (node.Test.Value == "none")
            {
                // Check if the operand is `null` (i.e., a NullValue)
                bool isNone = operand is NullValue;
                return new BooleanValue(node.Negate ? !isNone : isNone);
            }

            throw new Exception($"Unknown test: {node.Test.Value}");
        }

        private RuntimeValue EvaluateCallExpression(CallExpression expr, Environment env)
        {
            // Evaluate the function or method being called
            var callee = Evaluate(expr.Callee, env);

            // Ensure the callee is a callable function
            if (!(callee is FunctionValue function))
            {
                throw new Exception($"Cannot call a non-function: {callee.Type}, expected {expr.Callee.ToCode()} to be a defined function");
            }

            // Evaluate the arguments of the function call
            var args = new List<RuntimeValue>();
            for (int i = 0; i < expr.Arguments.Count; i++)
            {
                Expressions.Expression? arg = expr.Arguments[i];
                switch (arg)
                {
                    case KeywordArgumentExpression kw:

                        args.Add(new ArgumentValue(kw.Key.Value, Evaluate(kw.Value, env)));
                        break;
                    default:
                        args.Add(new ArgumentValue(i, Evaluate(arg, env)));
                        break;
                }
            }

            // Call the function with the evaluated arguments
            return function.Call(args, env);
        }

        private RuntimeValue EvaluateLiteral(LiteralExpression literal, Environment env)
        {
            return literal.Value switch
            {
                int i => new NumericValue(i),
                bool b => new BooleanValue(b),
                string s => new StringValue(s),
                _ => new NullValue(),
            };
        }

        private RuntimeValue EvaluateIf(IfStatement ifStatement, Environment env)
        {
            var testResult = Evaluate(ifStatement.Test, env);
            if (!(testResult is BooleanValue))
            {
                testResult = CoerceBooleanValue(testResult);
            }

            if (testResult is BooleanValue boolValue && (bool)boolValue.Value)
            {
                return EvaluateBlock(ifStatement.Body, env);
            }
            else
            {
                return EvaluateBlock(ifStatement.Alternate, env);
            }
        }

        private RuntimeValue EvaluateSet(SetStatement setStatement, Environment env)
        {
            var value = Evaluate(setStatement.Value, env);
            if (setStatement.Assignee is IdentifierExpression identifier)
            {
                env.Set(identifier.Value, value);
            }
            if (setStatement.Assignee is MemberExpression memberExpression)
            {
                var member = Evaluate(memberExpression.Object, env);
                var prop = ((IdentifierExpression)memberExpression.Property).Value;

                ((ObjectValue)member).Set(prop, value);

            }

            return value;
        }

        private RuntimeValue EvaluateFor(ForStatement forStatement, Environment env)
        {   // Create a new environment (scope) for the loop
            var loopEnv = env.CreateChildEnvironment();


            var iterable = Evaluate(forStatement.Iterable, env);
            if (iterable is ArrayValue array)
            {
                var rg = (Array)array.Value;
                var totalIterations = rg.Length;

                var result = new StringBuilder();
                System.Collections.IList list = rg;
                for (int i = 0; i < list.Count; i++)
                {
                    object? item = list[i];
                    loopEnv.Set(((IdentifierExpression)forStatement.LoopVar).Value, item);
                    // Define the `loop` object with properties like index0, index, first, last
                    var loopObject = new ObjectValue(new Dictionary<string, RuntimeValue>
                        {
                            { "index0", new NumericValue(i) },  // 0-based index
                            { "index", new NumericValue(i + 1) },  // 1-based index
                            { "first", new BooleanValue(i == 0) },  // First iteration
                            { "last", new BooleanValue(i == totalIterations - 1) },  // Last iteration
                            { "length", new NumericValue(totalIterations) }  // Total number of iterations
                        });

                    // Add the `loop` object to the environment
                    loopEnv.Set("loop", loopObject);

                    result.Append(EvaluateBlock(forStatement.Body, loopEnv).Value);
                }
                return new StringValue(result.ToString());
            }
            return new NullValue();
        }
        private RuntimeValue EvaluateMemberExpression(MemberExpression node, Environment env)
        {
            // Evaluate the object (e.g., the array or object we are accessing)
            var objectValue = Evaluate(node.Object, env);
            if (objectValue is UndefinedValue)
                throw new Exception($"{node.Object.ToCode()} identifier is unknow at runtime");


            // Handle array or computed access (like messages[2:])
            if (node.Computed)
            {
                RuntimeValue propertyValue = null;

                if (!(node.Property is SliceExpression))
                    propertyValue = Evaluate(node.Property, env);

                if (propertyValue is NumericValue numericIndex && objectValue is ArrayValue arrayValue)
                {
                    //throw new Exception("TODO Invalid array index or slicing expression");
                    // Access array by index
                    //return Environment.ConvertToRuntimeValue();
                    Array rg = (Array)arrayValue.Value;
                    int index = (int)numericIndex.Value;
                    if (index == -1)
                        index = rg.Length - 1;

                    var ret = rg.GetValue(index);
                    if (ret is RuntimeValue rv)
                        return rv;
                    return Environment.ConvertToRuntimeValue(ret);
                }
                if (propertyValue is StringValue stringIndex && objectValue is ObjectValue objValue)
                {
                    //throw new Exception("TODO Invalid array index or slicing expression");
                    // Access array by index
                    var ret = objValue.Get((string)stringIndex.Value);
                    if (ret is RuntimeValue rv)
                        return rv;
                    return Environment.ConvertToRuntimeValue(ret);
                }
                else if (node.Property is SliceExpression)
                {
                    //throw new Exception("TODO Invalid array index or slicing expression");
                    // Handle slicing
                    return EvaluateSliceExpression(node.Property as SliceExpression, env);
                }
                else
                {
                    throw new Exception("Invalid array index or slicing expression");
                }
            }
            else
            {
                // Handle object property access (e.g., object.property)
                if (objectValue is ObjectValue obj && node.Property is IdentifierExpression property)
                {
                    if (obj.value.TryGetValue(property.Value, out var value))
                    {
                        return value;
                    }
                    else
                    {
                        return new UndefinedValue();
                    }
                }
                else if (objectValue is StringValue str && node.Property is IdentifierExpression strip && strip.Value == "strip")
                {
                    return new FunctionValue((v, e) =>
                    {
                        return str;
                    });
                }

                throw new Exception("Invalid member access");
            }
        }

        private RuntimeValue EvaluateSliceExpression(SliceExpression node, Environment env)
        {
            // Evaluate the start, stop, and step values in the slice expression
            var start = node.Start != null ? Evaluate(node.Start, env) as NumericValue : null;
            var stop = node.Stop != null ? Evaluate(node.Stop, env) as NumericValue : null;
            var step = node.Step != null ? Evaluate(node.Step, env) as NumericValue : new NumericValue(1); // Default step is 1

            // The object to be sliced could be an array or string, so evaluate the object
            var objectValue = Evaluate(node.Object, env);

            if (objectValue is ArrayValue arrayValue)
            {
                return new ArrayValue(SliceArray((RuntimeValue[])arrayValue.Value, (int?)start?.Value, (int?)stop?.Value, (int)step?.Value).ToArray());
            }
            else if (objectValue is StringValue stringValue)
            {
                return new StringValue(SliceString((string)stringValue.Value, (int?)start?.Value, (int?)stop?.Value, (int)step?.Value));
            }
            else
            {
                throw new Exception("Slicing is only supported on arrays and strings");
            }
        }

        // Helper function to slice arrays
        private List<RuntimeValue> SliceArray(RuntimeValue[] array, int? start, int? stop, int step)
        {
            int arrayLength = array.Length;
            int actualStart = start ?? 0;
            int actualStop = stop ?? arrayLength;

            actualStart = actualStart < 0 ? Math.Max(arrayLength + actualStart, 0) : Math.Min(actualStart, arrayLength);
            actualStop = actualStop < 0 ? Math.Max(arrayLength + actualStop, 0) : Math.Min(actualStop, arrayLength);

            var slicedArray = new List<RuntimeValue>();

            for (int i = actualStart; i < actualStop; i += step)
            {
                slicedArray.Add(array[i]);
            }

            return slicedArray;
        }

        // Helper function to slice strings
        private string SliceString(string str, int? start, int? stop, int step)
        {
            int length = str.Length;
            int actualStart = start ?? 0;
            int actualStop = stop ?? length;

            actualStart = actualStart < 0 ? Math.Max(length + actualStart, 0) : Math.Min(actualStart, length);
            actualStop = actualStop < 0 ? Math.Max(length + actualStop, 0) : Math.Min(actualStop, length);

            var result = new StringBuilder();

            for (int i = actualStart; i < actualStop; i += step)
            {
                result.Append(str[i]);
            }

            return result.ToString();
        }

        private RuntimeValue EvaluateBinaryExpression(BinaryExpression binaryExpression, Environment env)
        {
            var left = Evaluate(binaryExpression.Left, env);
            var right = Evaluate(binaryExpression.Right, env);



            switch (binaryExpression.Operator)
            {
                case "in":
                    {
                        {
                            if (right is ArrayValue arrayValue)
                            {
                                bool result = false;
                                foreach (RuntimeValue rv in ((IEnumerable)arrayValue.Value))
                                {
                                    if (rv.Value.Equals(left.Value))
                                    {
                                        result = true;
                                        break;
                                    }
                                }
                                if (binaryExpression.Negated)
                                    return new BooleanValue(!result);
                                return new BooleanValue(result);

                            }
                        }

                        {
                            if (left is StringValue leftBool && right is StringValue rightBool)
                            {
                                var leftStr = (string)leftBool.Value;
                                var rightStr = (string)rightBool.Value;
                                return new BooleanValue(rightStr.Contains(leftStr));
                            };
                        }
                        {
                            if (left is StringValue leftBool && right is ObjectValue rightObj)
                            {
                                var leftStr = (string)leftBool.Value;
                                var r = rightObj.value;
                                if (r.Any(x => x.Value is StringValue rs && ((string)rs.Value).Contains(leftStr)))
                                    return new BooleanValue(true);
                                return new BooleanValue(false);
                            };
                        }

                    }
                    break;
                case "or":
                case "||":
                    {
                        left = CoerceBooleanValue(left);
                        right = CoerceBooleanValue(right);

                        if (left is BooleanValue leftBool && right is BooleanValue rightBool)
                        {
                            return new BooleanValue((bool)leftBool.Value || (bool)rightBool.Value);
                        };
                    }
                    break;
                case "and":
                case "&&":
                    {
                        left = CoerceBooleanValue(left);
                        right = CoerceBooleanValue(right);

                        if (left is BooleanValue leftBool && right is BooleanValue rightBool)
                        {
                            return new BooleanValue((bool)leftBool.Value && (bool)rightBool.Value);
                        };
                    }
                    break;
                case "%":
                    {
                        if (left is NumericValue leftNum && right is NumericValue rightNum)
                        {
                            return new NumericValue((int)leftNum.Value % (int)rightNum.Value);
                        };
                    }
                    break;
                case "+":
                    {
                        if (left is NumericValue leftNum && right is NumericValue rightNum)
                        {
                            return new NumericValue((int)leftNum.Value + (int)rightNum.Value);
                        }
                        else if (left is StringValue leftStr && right is StringValue rightStr)
                        {
                            return new StringValue((string)leftStr.Value + (string)rightStr.Value);
                        }
                    }
                    break;
                case "-":
                    if (left is NumericValue lNum && right is NumericValue rNum)
                    {
                        return new NumericValue((int)lNum.Value - (int)rNum.Value);
                    }
                    break;
                case "==":
                    return new BooleanValue(left.Value.Equals(right.Value));
                case "!=":
                    return new BooleanValue(!left.Value.Equals(right.Value));
                default:
                    throw new Exception($"Unknown binary operator: {binaryExpression.Operator}");
            }

            throw new Exception($"Invalid binary expression evaluation. {binaryExpression.ToCode()} left is {left} right is {right}");
        }

        private RuntimeValue CoerceBooleanValue(RuntimeValue left)
        {
            switch (left)
            {
                case ArrayValue a:
                    return new BooleanValue((a.Value as Array).Length > 0);
                case BooleanValue b:
                    return b;
                case NullValue _:
                    return new BooleanValue(false);
                case UndefinedValue _:
                    return new BooleanValue(false);
                default:
                    return new BooleanValue(true);
            }
        }
    }
}
