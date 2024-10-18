namespace GenAI.Server.Jinja.Expressions
{
    public class NullLiteral : LiteralExpression
    {
        public NullLiteral() : base(null) { }

        public override string Type => "NullLiteral";
    }
}
