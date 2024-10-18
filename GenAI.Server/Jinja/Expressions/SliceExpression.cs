using System.Text;

namespace GenAI.Server.Jinja.Expressions
{
    public class SliceExpression : Expression
    {
        public Expression Object { get; }
        public Expression Start { get; set; }
        public Expression Stop { get; set; }
        public Expression Step { get; set; }

        public SliceExpression(Expression @object, Expression start, Expression stop, Expression step)
        {
            Object = @object;
            Start = start;
            Stop = stop;
            Step = step;
        }

        public override string Type => "SliceExpression";
        public override string ToCode()
        {
            StringBuilder result = new();
            _ = result.Append(Object.ToCode());
            _ = result.Append("[");
            if (Start != null)
            {
                _ = result.Append(Start.ToCode());
            }
            _ = result.Append(":");
            if (Stop != null)
            {
                _ = result.Append(Stop.ToCode());
            }
            if (Step != null)
            {
                _ = result.Append(":");
                _ = result.Append(Step.ToCode());
            }
            _ = result.Append("]");
            return result.ToString();
        }
    }
}
