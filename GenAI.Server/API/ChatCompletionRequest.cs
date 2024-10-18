using System.Text.Json.Serialization;

namespace GenAI.Server.API
{
    public class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } // Model name

        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; } // The conversation history

        [JsonPropertyName("temperature")]
        public float Temperature { get; set; } = 1.0f; // Controls randomness

        [JsonPropertyName("top_p")]
        public float TopP { get; set; } = 1.0f; // Top-p (nucleus sampling)

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; } // Maximum number of tokens for the response

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false; // Whether to stream results or not

        [JsonPropertyName("frequency_penalty")]
        public float FrequencyPenalty { get; set; } = 0.0f; // Penalize frequent tokens
    }
}
