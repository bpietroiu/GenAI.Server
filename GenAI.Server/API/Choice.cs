using System.Text.Json.Serialization;

namespace GenAI.Server.API
{
    public class Choice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; } // The choice index

        [JsonPropertyName("message")]
        public Message Message { get; set; } // The message returned by the model

        [JsonPropertyName("finish_reason")]
        [JsonConverter(typeof(JsonStringEnumConverter))] // Serialize the FinishReason enum as a string
        public FinishReason FinishReason { get; set; } // The reason the generation stopped
    }
}
