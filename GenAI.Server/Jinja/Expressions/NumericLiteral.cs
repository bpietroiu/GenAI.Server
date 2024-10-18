namespace GenAI.Server.Jinja.Expressions
{
    public class NumericLiteral : LiteralExpression
    {
        public NumericLiteral(int value) : base(value) { }

        public override string Type => "NumericLiteral";
    }
}
