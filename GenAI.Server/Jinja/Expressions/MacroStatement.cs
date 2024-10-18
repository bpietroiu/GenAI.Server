using System.Text;

namespace GenAI.Server.Jinja.Expressions
{
    public class MacroStatement : Statement
    {
        public Expression Name { get; }
        public List<Expression> Args { get; }
        public List<Statement> Body { get; }

        public MacroStatement(Expression name, List<Expression> args, List<Statement> body)
        {
            Name = name;
            Args = args;
            Body = body;
        }

        public override string ToCode()
        {
            StringBuilder result = new();
            _ = result.Append("def ");
            _ = result.Append(Name.ToCode());
            _ = result.Append("(");

            for (int i = 0; i < Args.Count; i++)
            {
                _ = result.Append(Args[i].ToCode());
                if (i < Args.Count - 1)
                {
                    _ = result.Append(", ");
                }
            }

            _ = result.Append("):\n");

            foreach (Statement statement in Body)
            {
                _ = result.Append("    ");
                _ = result.Append(statement.ToCode());
                _ = result.Append("\n");
            }

            return result.ToString();
        }
    }
}
