using System.Text;

namespace GenAI.Server.Jinja.Expressions
{
    // Binary Expression
    public class BinaryExpression : Expression
    {
        public bool Negated { get; set; }
        public string Operator { get; set; }
        public Expression Left { get; set; }
        public Expression Right { get; set; }

        public BinaryExpression(string operatorValue, Expression left, Expression right)
        {
            Operator = operatorValue;
            Left = left;
            Right = right;
        }

        public override string Type => "BinaryExpression";

        public override string ToCode()
        {
            StringBuilder result = new();
            _ = result.Append(Left.ToCode());
            if(Negated)
            {
                _ = result.Append(" not");
            }
            _ = result.Append($" {Operator} ");
            _ = result.Append(Right.ToCode());
            return result.ToString();
        }
    }
}
