namespace GenAI.Server.Jinja.Runtime
{
    public class NumericValue : RuntimeValue
    {
        public NumericValue(int value)
        {
            Type = "NumericValue";
            Value = value;
        }

        public static RuntimeValue Zero { get; internal set; } = new NumericValue(0);
        public override object Value { get; }
    }
}
