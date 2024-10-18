namespace GenAI.Server.Jinja.Runtime
{
    public class UndefinedValue : RuntimeValue
    {
        public override object? Value => null;

        public UndefinedValue()
        {
            Type = "UndefinedValue";
        }
    }
}
