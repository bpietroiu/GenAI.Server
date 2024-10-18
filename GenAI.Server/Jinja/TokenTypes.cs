namespace GenAI.Server.Jinja
{
    public static class TokenTypes
    {
        public const string Text = "Text";
        public const string NumericLiteral = "NumericLiteral";
        public const string BooleanLiteral = "BooleanLiteral";
        public const string NullLiteral = "NullLiteral";
        public const string StringLiteral = "StringLiteral";
        public const string Identifier = "Identifier";
        public new const string Equals = "Equals";
        public const string OpenParen = "OpenParen";
        public const string CloseParen = "CloseParen";
        public const string OpenStatement = "OpenStatement";
        public const string CloseStatement = "CloseStatement";
        public const string OpenExpression = "OpenExpression";
        public const string CloseExpression = "CloseExpression";
        public const string OpenSquareBracket = "OpenSquareBracket";
        public const string CloseSquareBracket = "CloseSquareBracket";
        public const string OpenCurlyBracket = "OpenCurlyBracket";
        public const string CloseCurlyBracket = "CloseCurlyBracket";
        public const string Comma = "Comma";
        public const string Dot = "Dot";
        public const string Colon = "Colon";
        public const string Pipe = "Pipe";
        public const string CallOperator = "CallOperator";
        public const string AdditiveBinaryOperator = "AdditiveBinaryOperator";
        public const string MultiplicativeBinaryOperator = "MultiplicativeBinaryOperator";
        public const string ComparisonBinaryOperator = "ComparisonBinaryOperator";
        public const string UnaryOperator = "UnaryOperator";

        // Keywords
        public const string Set = "Set";
        public const string If = "If";
        public const string For = "For";
        public const string In = "In";
        public const string Is = "Is";
        public const string NotIn = "NotIn";
        public const string Else = "Else";
        public const string EndIf = "EndIf";
        public const string ElseIf = "ElseIf";
        public const string EndFor = "EndFor";
        public const string And = "And";
        public const string Or = "Or";
        public const string Not = "Not";
        public const string Macro = "Macro";
        public const string EndMacro = "EndMacro";
    }
}
