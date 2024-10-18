using System.Text;

namespace GenAI.Server.Jinja.Expressions
{
    public class SetStatement : Statement
    {
        public Expression Assignee { get; }
        public Statement Value { get; }

        public SetStatement(Expression assignee, Statement value)
        {
            Assignee = assignee;
            Value = value;
        }

        public override string ToCode()
        {
            StringBuilder result = new();
            _ = result.Append(Assignee.ToCode());
            _ = result.Append(" = ");
            _ = result.Append(Value.ToCode());
            return result.ToString();
        }
    }
}
