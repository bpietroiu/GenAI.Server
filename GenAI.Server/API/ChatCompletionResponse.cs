using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GenAI.Server.API
{
    public class CompletionUsage
    {
        /// <summary>
        /// Number of tokens in the generated completion.
        /// </summary>
        /// <value>Number of tokens in the generated completion.</value>
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        /// <summary>
        /// Number of tokens in the prompt.
        /// </summary>
        /// <value>Number of tokens in the prompt.</value>
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        /// <summary>
        /// Total number of tokens used in the request (prompt + completion).
        /// </summary>
        /// <value>Total number of tokens used in the request (prompt + completion).</value>
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    public class ChatCompletionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } // Unique request ID

        [JsonPropertyName("object")]
        public string Object { get; set; } = "chat.completion"; // Fixed value

        [JsonPropertyName("created")]
        public long Created { get; set; } // Unix timestamp when the request was processed

        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; } // The choices returned by the model

        [JsonPropertyName("usage")]
        public CompletionUsage Usage { get; set; } // The token usage statistics
    }
}
