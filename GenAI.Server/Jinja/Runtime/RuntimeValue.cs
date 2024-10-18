namespace GenAI.Server.Jinja.Runtime
{
    // Runtime value classes
    public abstract class RuntimeValue
    {
        public virtual string Type { get; set; }
        public abstract object Value { get; }
    }

    public class ArgumentValue : RuntimeValue
    {
        public string Name { get; }
        public int Position { get; set; }

        public ArgumentValue(string name, RuntimeValue value)
        {
            Name = name;
            Value = value;
        }

        public ArgumentValue(int position, RuntimeValue value)
        {
            Position = position;
            Value = value;
        }

        public override string Type => "Argument";
        public override object Value { get; }
    }
}
