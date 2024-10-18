namespace GenAI.Server.Jinja.Runtime
{
    public class StringValue : RuntimeValue
    {
        public StringValue(string value)
        {
            Type = "StringValue";
            Value = value;
        }

        public override object Value { get; }
    }
}
