namespace GenAI.Server.Jinja.Runtime
{
    public class NullValue : RuntimeValue
    {
        public override object? Value => null;

        public NullValue()
        {
            Type = "NullValue";
        }
    }
}
