namespace GenAI.Server.Jinja.Expressions
{
    public class BreakStatement : Statement
    {
        public override string ToCode() => "break;";
    }
}
