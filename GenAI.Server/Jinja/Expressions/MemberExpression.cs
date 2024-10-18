using System.Text;

namespace GenAI.Server.Jinja.Expressions
{
    public class MemberExpression : Expression
    {
        public Expression Object { get; set; }
        public Expression Property { get; set; }
        public bool Computed { get; set; } // Whether the property is computed (e.g., array[index])

        public MemberExpression(Expression obj, Expression property, bool computed)
        {
            Object = obj;
            Property = property;
            Computed = computed;
        }

        public override string Type => "MemberExpression";
        public override string ToCode()
        {
            StringBuilder result = new();
            _ = result.Append(Object.ToCode());
            if (Computed)
            {
                _ = result.Append("[");
                _ = result.Append(Property.ToCode());
                _ = result.Append("]");
            }
            else
            {
                _ = result.Append(".");
                _ = result.Append(Property.ToCode());
            }
            return result.ToString();
        }
    }
}
