namespace GenAI.Server.Jinja
{
    // Error handling and utility classes
    public class SyntaxError : Exception
    {
        public SyntaxError(string message) : base(message) { }
    }
}
