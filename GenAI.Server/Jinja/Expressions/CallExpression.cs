using System.Text;

namespace GenAI.Server.Jinja.Expressions
{
    public class CallExpression : Expression
    {
        public Expression Callee { get; set; }
        public List<Expression> Arguments { get; set; }

        public CallExpression(Expression callee, List<Expression> arguments)
        {
            Callee = callee;
            Arguments = arguments;
        }

        public override string Type => "CallExpression";
        public override string ToCode()
        {
            StringBuilder result = new();
            _ = result.Append(Callee.ToCode());
            _ = result.Append("(");

            for (int i = 0; i < Arguments.Count; i++)
            {
                _ = result.Append(Arguments[i].ToCode());
                if (i < Arguments.Count - 1)
                {
                    _ = result.Append(", ");
                }
            }

            _ = result.Append(")");
            return result.ToString();
        }
    }
}
