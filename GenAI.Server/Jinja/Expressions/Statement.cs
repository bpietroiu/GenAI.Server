namespace GenAI.Server.Jinja.Expressions
{
    public abstract class Statement
    {
        public abstract string ToCode();

        public virtual string Type { get; set; } = "Statement";
    }
}
