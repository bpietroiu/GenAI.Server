namespace GenAI.Server.Jinja
{
    public static class OrderedMappingTable
    {
        public static readonly List<Tuple<string, string>> Mappings =
    [
        Tuple.Create("{%-", TokenTypes.OpenStatement),
        Tuple.Create("{%", TokenTypes.OpenStatement),
        Tuple.Create("%}", TokenTypes.CloseStatement),
        Tuple.Create("{{-", TokenTypes.OpenExpression),
        Tuple.Create("{{", TokenTypes.OpenExpression),
        Tuple.Create("}}", TokenTypes.CloseExpression),
        Tuple.Create("(", TokenTypes.OpenParen),
        Tuple.Create(")", TokenTypes.CloseParen),
        Tuple.Create("{", TokenTypes.OpenCurlyBracket),
        Tuple.Create("}", TokenTypes.CloseCurlyBracket),
        Tuple.Create("[", TokenTypes.OpenSquareBracket),
        Tuple.Create("]", TokenTypes.CloseSquareBracket),
        Tuple.Create(",", TokenTypes.Comma),
        Tuple.Create(".", TokenTypes.Dot),
        Tuple.Create(":", TokenTypes.Colon),
        Tuple.Create("|", TokenTypes.Pipe),
        Tuple.Create("<=", TokenTypes.ComparisonBinaryOperator),
        Tuple.Create(">=", TokenTypes.ComparisonBinaryOperator),
        Tuple.Create("==", TokenTypes.ComparisonBinaryOperator),
        Tuple.Create("!=", TokenTypes.ComparisonBinaryOperator),
        Tuple.Create("<", TokenTypes.ComparisonBinaryOperator),
        Tuple.Create(">", TokenTypes.ComparisonBinaryOperator),
        Tuple.Create("+", TokenTypes.AdditiveBinaryOperator),
        Tuple.Create("-", TokenTypes.AdditiveBinaryOperator),
        Tuple.Create("*", TokenTypes.MultiplicativeBinaryOperator),
        Tuple.Create("/", TokenTypes.MultiplicativeBinaryOperator),
        Tuple.Create("%", TokenTypes.MultiplicativeBinaryOperator),
        Tuple.Create("=", TokenTypes.Equals)
    ];
    }
}
