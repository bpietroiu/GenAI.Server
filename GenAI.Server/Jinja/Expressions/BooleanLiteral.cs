namespace GenAI.Server.Jinja.Expressions
{
    public class BooleanLiteral : LiteralExpression
    {
        public BooleanLiteral(bool value) : base(value) { }

        public override string Type => "BooleanLiteral";
    }
}
