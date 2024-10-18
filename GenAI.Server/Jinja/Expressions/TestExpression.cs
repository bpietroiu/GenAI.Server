using System.Text;

namespace GenAI.Server.Jinja.Expressions
{
    public class TestExpression : Expression
    {
        public Expression Operand { get; set; }  // The value being tested (e.g., `system_message`)
        public bool Negate { get; set; }         // Whether this is a negated test (e.g., `is not defined`)
        public IdentifierExpression Test { get; set; }  // The test type (e.g., `defined`, `none`)

        public TestExpression(Expression operand, bool negate, IdentifierExpression test)
        {
            Operand = operand;
            Negate = negate;
            Test = test;
        }

        public override string Type => "TestExpression";
        public override string ToCode()
        {
            StringBuilder result = new();
            _ = result.Append(Operand.ToCode());
            _ = result.Append(" is ");
            if (Negate)
            {
                _ = result.Append("not ");
            }
            _ = result.Append(Test.ToCode());
            return result.ToString();
        }
    }
}
