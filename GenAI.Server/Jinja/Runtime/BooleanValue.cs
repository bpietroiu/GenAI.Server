namespace GenAI.Server.Jinja.Runtime
{
    public class BooleanValue : RuntimeValue
    {
        public BooleanValue(bool value)
        {
            Type = "BooleanValue";
            Value = value;
        }

        public override object Value { get; }
    }
}
