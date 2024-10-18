namespace GenAI.Server.Jinja
{
    public static class Keywords
    {
        public static readonly Dictionary<string, string> Mapping = new()
        {
        { "set", TokenTypes.Set },
        { "for", TokenTypes.For },
        { "in", TokenTypes.In },
        { "is", TokenTypes.Is },
        { "if", TokenTypes.If },
        { "else", TokenTypes.Else },
        { "endif", TokenTypes.EndIf },
        { "elif", TokenTypes.ElseIf },
        { "endfor", TokenTypes.EndFor },
        { "and", TokenTypes.And },
        { "or", TokenTypes.Or },
        { "not", TokenTypes.Not },
        { "not in", TokenTypes.NotIn },
        { "macro", TokenTypes.Macro },
        { "endmacro", TokenTypes.EndMacro },
        { "true", TokenTypes.BooleanLiteral },
        { "false", TokenTypes.BooleanLiteral },
        { "none", TokenTypes.NullLiteral },
        { "True", TokenTypes.BooleanLiteral },
        { "False", TokenTypes.BooleanLiteral },
        { "None", TokenTypes.NullLiteral }
    };
    }
}
