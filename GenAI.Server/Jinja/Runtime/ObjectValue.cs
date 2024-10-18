namespace GenAI.Server.Jinja.Runtime
{
    public class ObjectValue : RuntimeValue
    {
        // Dictionary to hold key-value pairs for the object
        public Dictionary<string, RuntimeValue> value { get; private set; }

        // Constructor to create an object with an initial set of key-value pairs
        public ObjectValue(Dictionary<string, RuntimeValue>? initialValues = null)
        {
            value = initialValues ?? [];
        }

        public override string Type => "ObjectValue";


        // Method to set a key-value pair in the object
        public void Set(string key, RuntimeValue value_)
        {
            value[key] = value_;
        }

        // Method to get the value associated with a key in the object
        public RuntimeValue Get(string key)
        {
            if (value.TryGetValue(key, out RuntimeValue? result))
            {
                return result;
            }
            return new UndefinedValue(); // Return Undefined if the key does not exist
        }

        public override object Value => value;
    }
}
