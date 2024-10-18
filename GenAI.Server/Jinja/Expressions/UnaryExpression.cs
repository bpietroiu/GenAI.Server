using System.Text;

namespace GenAI.Server.Jinja.Expressions
{
    public class UnaryExpression : Expression
    {
        public string Operator { get; set; }     // The unary operator, e.g., "!", "-" 
        public Expression Argument { get; set; } // The expression that the operator applies to

        public UnaryExpression(string operatorValue, Expression argument)
        {
            Operator = operatorValue;
            Argument = argument;
        }

        public override string Type => "UnaryExpression";
        public override string ToCode()
        {
            StringBuilder result = new();
            _ = result.Append($"{Operator} ");
            _ = result.Append(Argument.ToCode());
            return result.ToString();
        }
    }
}
