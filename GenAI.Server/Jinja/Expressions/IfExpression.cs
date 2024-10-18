using System.Text;

namespace GenAI.Server.Jinja.Expressions
{
    public class IfExpression : Expression
    {
        public Expression Test { get; set; }       // Condition (the 'b' in 'a if b else c')
        public Expression Consequent { get; set; } // Expression if condition is true (the 'a' in 'a if b else c')
        public Expression Alternate { get; set; }  // Expression if condition is false (the 'c' in 'a if b else c')

        public IfExpression(Expression test, Expression consequent, Expression alternate)
        {
            Test = test;
            Consequent = consequent;
            Alternate = alternate;
        }

        public override string Type => "IfExpression";

        public override string ToCode()
        {
            StringBuilder result = new();
            _ = result.Append(Consequent.ToCode());
            _ = result.Append(" if ");
            _ = result.Append(Test.ToCode());
            _ = result.Append(" else ");
            _ = result.Append(Alternate.ToCode());
            return result.ToString();
        }
    }
}
