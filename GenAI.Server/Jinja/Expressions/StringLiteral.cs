namespace GenAI.Server.Jinja.Expressions
{
    public class StringLiteral : LiteralExpression
    {
        public StringLiteral(string value) : base(value) { }

        public override string Type => "StringLiteral";
    }
}
