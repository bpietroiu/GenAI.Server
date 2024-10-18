namespace GenAI.Server.Jinja
{
    public static class EscapeCharacters
    {
        public static readonly Dictionary<string, string> Mappings = new()
        {
        { "n", "\n" },
        { "t", "\t" },
        { "r", "\r" },
        { "b", "\b" },
        { "f", "\f" },
        { "v", "\v" },
        { "'", "'" },
        { "\"", "\"" },
        { "\\", "\\" }
    };
    }
}
