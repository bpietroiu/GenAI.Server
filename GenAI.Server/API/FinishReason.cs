using System.Text.Json.Serialization;

namespace GenAI.Server.API
{
    public enum FinishReason
    {
        [JsonPropertyName("stop")]
        Stop,
        [JsonPropertyName("length")]
        Length,
        [JsonPropertyName("function_call")]
        FunctionCall
    }
}
