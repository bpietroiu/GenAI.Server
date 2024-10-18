namespace GenAI.Server.Jinja.Expressions
{
    // Array Literal
    public class ArrayLiteral : LiteralExpression
    {
        public ArrayLiteral(List<Expression> elements) : base(elements) { }

        public override string Type => "ArrayLiteral";
    }
}
