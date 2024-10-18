using System.Text.Json.Serialization;

namespace GenAI.Server.API
{
    public class Message
    {
        [JsonPropertyName("role")]
        [JsonConverter(typeof(JsonStringEnumConverter))] // Serialize the Role enum as a string
        public Role Role { get; set; } // The role of the sender (assistant, user, system)

        [JsonPropertyName("content")]
        public string Content { get; set; } // The message content
    }
}
