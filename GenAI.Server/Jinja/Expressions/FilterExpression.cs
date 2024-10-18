namespace GenAI.Server.Jinja.Expressions
{
    public class FilterExpression : Expression
    {
        public Expression BaseExpression { get; set; }  // The expression to which the filter is applied
        public IdentifierExpression FilterName { get; set; }  // The filter being applied (e.g., `upper`, `trim`)
        public List<Expression> Arguments { get; set; }  // Optional arguments for the filter (if any)

        // Constructor
        public FilterExpression(Expression baseExpression, IdentifierExpression filterName, List<Expression> arguments)
        {
            BaseExpression = baseExpression;
            FilterName = filterName;
            Arguments = arguments ?? new List<Expression>();  // Initialize arguments to an empty list if null
        }

        // The expression type (for debugging/inspection)
        public override string Type => "FilterExpression";

        public override string ToCode()
        {
            // Convert the filter expression to code
            string args = string.Join(", ", Arguments.Select(arg => arg.ToCode()));
            return $"{BaseExpression.ToCode()}|{FilterName.ToCode()}({args})";
        }
    }
}
