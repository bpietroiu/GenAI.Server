namespace GenAI.Server.Jinja.Runtime
{
    public class ArrayValue : RuntimeValue
    {
        public ArrayValue(RuntimeValue[] value)
        {
            Type = "ArrayValue";
            Value = value;
        }

        public override object Value { get; }
    }
}
