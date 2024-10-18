namespace GenAI.Server.Jinja.Expressions
{
    public class IdentifierExpression : Expression
    {
        public string Value { get; }

        public IdentifierExpression(string value)
        {
            Value = value;
        }
        public override string ToCode()
        {
            return Value;
        }
    }
}
