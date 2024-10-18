using System.Text;

namespace GenAI.Server.Jinja.Expressions
{
    public class IfStatement : Expression
    {
        public Expression Test { get; }
        public List<Statement> Body { get; }
        public List<Statement> Alternate { get; }

        public IfStatement(Expression test, List<Statement> body, List<Statement> alternate)
        {
            Test = test;
            Body = body;
            Alternate = alternate;
        }

        public override string ToCode()
        {
            StringBuilder result = new();
            _ = result.Append("if ");
            _ = result.Append(Test.ToCode());
            _ = result.Append(":\n");

            foreach (Statement statement in Body)
            {
                _ = result.Append("    ");
                _ = result.Append(statement.ToCode());
                _ = result.Append("\n");
            }

            if (Alternate.Count > 0)
            {
                _ = result.Append("else:\n");
                foreach (Statement statement in Alternate)
                {
                    _ = result.Append("    ");
                    _ = result.Append(statement.ToCode());
                    _ = result.Append("\n");
                }
            }

            return result.ToString();
        }
    }
}
