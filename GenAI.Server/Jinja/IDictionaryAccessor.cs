using GenAI.Server.Jinja.Runtime;

namespace GenAI.Server.Jinja
{
    public interface IDictionaryAccessor
    {
        RuntimeValue Get(string key);
        void Set(string key, RuntimeValue value);
    }
}
