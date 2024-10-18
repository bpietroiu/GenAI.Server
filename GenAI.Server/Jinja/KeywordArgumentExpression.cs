using GenAI.Server.Jinja.Expressions;

namespace GenAI.Server.Jinja
{
    public class KeywordArgumentExpression : Expression
    {
        public IdentifierExpression Key { get; set; }
        public Expression Value { get; set; }

        public KeywordArgumentExpression(IdentifierExpression key, Expression value)
        {
            Key = key;
            Value = value;
        }

        public override string Type => "KeywordArgumentExpression";
        public override string ToCode()
        {
            return $"{Key.ToCode()}={Value.ToCode()}";
        }
    }

}
