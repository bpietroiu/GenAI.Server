namespace GenAI.Server.Jinja
{
    public class Token
    {
        public string Value { get; }
        public string Type { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(string value, string type, int line, int column)
        {
            Value = value;
            Type = type;
            Line = line;
            Column = column;
        }
    }
}
