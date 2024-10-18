namespace GenAI.Server.Jinja.Expressions
{
    // Object Literal
    public class ObjectLiteral : LiteralExpression
    {
        public ObjectLiteral(Dictionary<Expression, Expression> values) : base(values) { }

        public override string Type => "ObjectLiteral";
    }
}
