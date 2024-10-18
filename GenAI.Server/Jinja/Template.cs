using GenAI.Server.Jinja.Runtime;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;

namespace GenAI.Server.Jinja
{
    public class Template
    {
        private readonly ProgramNode _parsedTemplate;

        public Template(string template)
        {
            var tokens = TemplateProcessor.Tokenize(template, new PreprocessOptions { LstripBlocks = true, TrimBlocks = true });
            _parsedTemplate = Parser.Parse(tokens);
        }

        public string Render(Dictionary<string, object> context)
        {
            var environment = new Environment();
            foreach (var kvp in context)
            {
                environment.Set(kvp.Key, kvp.Value);
            }

            environment.Set("raise_exception", FiltersBuilder.BuildRaiseException);

            environment.Set("selectattr", FiltersBuilder.BuildSelectAttr);
            environment.Set("list", FiltersBuilder.BuildList);
            environment.Set("namespace", FiltersBuilder.BuildNamespace);
            environment.Set("trim", FiltersBuilder.BuildTrim);
            environment.Set("length", FiltersBuilder.BuildLength);
            environment.Set("tojson", FiltersBuilder.BuildToJSON);

            var interpreter = new Interpreter(environment);
            var result = interpreter.Run(_parsedTemplate);
            return result.Value.ToString();
        }
    }

    class FiltersBuilder
    {
        public static FunctionValue BuildSelectAttr
        => new FunctionValue((args, env) =>
        {
            if (args.Count != 4
            || !(args[1] is StringValue)
            || !(args[2] is StringValue)
            || !(args[3] is StringValue)
            )
            {
                throw new Exception("selectattr() expects 4 string argument");
            }

            var targetCollection = (Array)((ArrayValue)args[0]).Value;
            var attrName = (string)((StringValue)args[1]).Value;
            var @operator = (string)((StringValue)args[2]).Value;
            var value = (string)((StringValue)args[3]).Value;

            List<RuntimeValue> result = new();

            foreach (var item in targetCollection)
            {
                var itemDict = (ObjectValue)item;
                var attrValue = itemDict.Get(attrName);
                if (attrValue is not UndefinedValue)
                {
                    switch (@operator)
                    {
                        case "equalto":
                            if (attrValue.Value?.ToString() == value)
                            {
                                result.Add(itemDict);
                            }
                            break;
                        default:
                            throw new Exception($"Unsupported operator {@operator} for equalto filter");
                    }
                }
            }

            return new ArrayValue(result.ToArray());
        });

        public static FunctionValue BuildToJSON => new FunctionValue((args, env) =>
        {
            int indent = 0;

            var namedArgs = args.Skip(1).Cast<ArgumentValue>().Where(x => !string.IsNullOrEmpty(x.Name)).ToDictionary(x => x.Name, y => y.Value);
            if (namedArgs.TryGetValue("indent", out var indentValue) && indentValue is NumericValue numIndentValue)
            {
                indent = (int)numIndentValue.Value;
            }


            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            };

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);

            using (var writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                writer.Indentation = indent; // Set the indentation to 6 spaces

                var serializer = Newtonsoft.Json.JsonSerializer.Create(settings);
                var dict = (IDictionary<string, RuntimeValue>)args[0].Value;
                serializer.Serialize(writer, dict.ToDictionary(x => x.Key, x => x.Value.Value));
            }

            string json = sb.ToString();

            return new StringValue(json);
        });

        public static FunctionValue BuildTrim
    => new FunctionValue((args, env) =>
    {
        if (args.Count != 1 || !(args[0] is StringValue))
        {
            throw new Exception("trim() expects one string argument");
        }

        var str1 = (string)((StringValue)args[0]).Value;
        return new StringValue(str1.Trim());
    });
        public static FunctionValue BuildLength
    => new FunctionValue((args, env) =>
    {
        if (!(args.Count == 1 && (args[0] is StringValue || args[0] is ArrayValue)))
        {
            throw new Exception("length() expects one string or array argument");
        }

        if (args[0] is ArrayValue arrayValue)
        {
            return new NumericValue(((Array)arrayValue.Value).Length);
        }

        var str1 = (string)((StringValue)args[0]).Value;
        return new NumericValue(str1.Length);
    });

        public static FunctionValue BuildRaiseException
            => new FunctionValue((args, env) =>
            {
                if (args.Count != 1 || !(args[0] is StringValue))
                {
                    throw new Exception("raise_exception() expects one string argument");
                }

                var str1 = ((StringValue)args[0]).Value;
                throw new Exception((string)str1);
            });

        public static FunctionValue BuildList
            => new FunctionValue((args, env) =>
            {
                if (args.Count != 1)
                {
                    throw new Exception("list() expects one argument");
                }

                if (args[0] is ArrayValue arrayValue)
                {
                    return arrayValue;
                }

                return new ArrayValue(new[] { args[0] });
            });

        public static FunctionValue BuildNamespace
            => new FunctionValue((args, env) =>
            {
                List<ArgumentValue> positionalArgs = args.Cast<ArgumentValue>().Where(x => string.IsNullOrEmpty(x.Name)).ToList();
                List<ArgumentValue> namedArgs = args.Cast<ArgumentValue>().Where(x => !string.IsNullOrEmpty(x.Name)).ToList();

                if (positionalArgs.Count != 0)
                {
                    throw new Exception("namespace() expects no positional arguments");
                }

                return new ObjectValue(namedArgs.ToDictionary(x => x.Name, y => (RuntimeValue)y.Value));
            });
    }
}
