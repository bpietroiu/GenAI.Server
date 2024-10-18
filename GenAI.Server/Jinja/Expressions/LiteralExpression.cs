namespace GenAI.Server.Jinja.Expressions
{
    public class LiteralExpression : Expression
    {
        public object Value { get; }

        public LiteralExpression(object value)
        {
            Value = value;
        }

        public override string ToCode()
        {
            return Value.ToString();
        }
    }
}
