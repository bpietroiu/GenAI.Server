namespace GenAI.Server.Jinja.Runtime
{
    public class FunctionValue : RuntimeValue
    {
        public Func<List<RuntimeValue>, Environment, RuntimeValue> Function { get; }

        public FunctionValue(Func<List<RuntimeValue>, Environment, RuntimeValue> function)
        {
            Function = function;
        }

        public override string Type => "Function";

        // Call the function with the provided arguments and environment
        public RuntimeValue Call(List<RuntimeValue> args, Environment env)
        {
            return Function(args, env);
        }
        public override object Value => Function;
    }
}
