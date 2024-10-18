using System.ComponentModel;
using GenAI.Server.Jinja.Runtime;

namespace GenAI.Server.Jinja
{
    // Additional AST classes as needed for If, For, etc.

    // The runtime execution environment
    public class Environment
    {    // Constructor to create a new environment, optionally with a parent environment
        public Environment(Environment parent = null)
        {
            _parent = parent;
        }
        private readonly Environment _parent;  // Parent environment for nested scopes

        public Dictionary<string, RuntimeValue> Variables { get; set; } = [];

        public void Set(string name, object value)
        {
            if (value is RuntimeValue rv)
            {
                Variables[name] = rv;
            }
            else
            {
                Variables[name] = value is FunctionValue func ? func : ConvertToRuntimeValue(value);
            }
        }

        public RuntimeValue LookupVariable(string name)
        {
            if (Variables.TryGetValue(name, out var value))
            {
                return value;
            }
            else if (_parent != null)
            {
                return _parent.LookupVariable(name);  // Recursively look in the parent environment
            }

            return new UndefinedValue();  // Return undefined if the variable is not found

        }
        public static Dictionary<string, RuntimeValue> ToDictionary(object values)
        {
            Dictionary<string, RuntimeValue> dict = new(StringComparer.OrdinalIgnoreCase);

            if (values != null)
            {
                foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(values))
                {
                    object obj = propertyDescriptor.GetValue(values);
                    dict[propertyDescriptor.Name] = ConvertToRuntimeValue(obj);
                }
            }

            return dict;
        }

        public static RuntimeValue ConvertToRuntimeValue(object value)
        {
            // Convert primitive values into RuntimeValue objects
            return value switch
            {
                int i => new NumericValue(i),
                string s => new StringValue(s),
                bool b => new BooleanValue(b),
                null => new NullValue(),
                Array rg => new ArrayValue((rg as object[]).ToList().Select(ConvertToRuntimeValue).ToArray()),
                object o => new ObjectValue(ToDictionary(o)),
            };
        }

        // Method to check if a variable is defined in the current scope or parent scopes
        public bool IsVariableDefined(string name)
        {
            if (Variables.ContainsKey(name))
            {
                return true;
            }
            else if (_parent != null)
            {
                return _parent.IsVariableDefined(name);  // Check in the parent environment
            }

            return false;
        }

        // Create a new environment that inherits from the current one (e.g., for a loop)
        public Environment CreateChildEnvironment()
        {
            return new Environment(this);  // The current environment becomes the parent of the new one
        }

        // Helper method to set multiple variables at once (e.g., in a loop context)
        public void SetVariables(Dictionary<string, RuntimeValue> variables)
        {
            foreach (var variable in variables)
            {
                Set(variable.Key, variable.Value);
            }
        }
    }
}
