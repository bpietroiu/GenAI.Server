using System.Text;

namespace GenAI.Server.Jinja.Expressions
{


    public class ForStatement : Statement
    {
        public Expression LoopVar { get; }
        public Expression Iterable { get; }
        public List<Statement> Body { get; }
        public List<Statement> DefaultBlock { get; }

        public ForStatement(Expression loopVar, Expression iterable, List<Statement> body, List<Statement> defaultBlock)
        {
            LoopVar = loopVar;
            Iterable = iterable;
            Body = body;
            DefaultBlock = defaultBlock;
        }

        public override string ToCode()
        {
            StringBuilder result = new();
            _ = result.Append("for ");
            _ = result.Append(LoopVar.ToCode());
            _ = result.Append(" in ");
            _ = result.Append(Iterable.ToCode());
            _ = result.Append(":\n");

            foreach (Statement statement in Body)
            {
                _ = result.Append("    ");
                _ = result.Append(statement.ToCode());
                _ = result.Append("\n");
            }

            if (DefaultBlock.Count > 0)
            {
                _ = result.Append("else:\n");
                foreach (Statement statement in DefaultBlock)
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
