namespace GenAI.Server.Jinja.Expressions
{
    // Tuple Literal
    public class TupleLiteral : LiteralExpression
    {
        public TupleLiteral(List<Expression> elements) : base(elements) { }

        public override string Type => "TupleLiteral";
    }
}
