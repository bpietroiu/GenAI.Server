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

        /// <summary>
        /// Gets or Sets name for tool calls
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("tool_calls")]
        public List<FunctionContent>? ToolCalls { get; set; }

        [JsonPropertyName("tool_call_id")]
        public string? ToolCallId { get; set; }
    }

    public class FunctionContent
    {
        public FunctionContent(string id, FunctionCall function)
        {
            this.Function = function;
            this.Id = id;
        }

        [JsonPropertyName("function")]
        public FunctionCall Function { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        public class FunctionCall
        {
            public FunctionCall(string name, Dictionary<string, object> arguments)
            {
                this.Name = name;
                this.Arguments = arguments;
            }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("arguments")]
            public Dictionary<string, object> Arguments { get; set; }
        }
    }

}
