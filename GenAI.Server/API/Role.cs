using System.Text.Json.Serialization;

namespace GenAI.Server.API
{
    public enum Role
    {
        [JsonPropertyName("system")]
        System,
        [JsonPropertyName("user")]
        User,
        [JsonPropertyName("assistant")]
        Assistant
    }
}
