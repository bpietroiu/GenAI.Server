using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;

namespace GenAI.Server.API
{
    public abstract class ToolBase
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        public ToolBase(string type)
        {
            Type = type;
        }
    }

    public class FunctionDefinition
    {
        public FunctionDefinition(string name, string description, JsonSchema? parameters = default)
        {
            Name = name;
            Description = description;
            Parameters = parameters;
        }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("parameters")]
        public JsonSchema? Parameters { get; set; }
    }

    public class FunctionTool : ToolBase
    {
        public FunctionTool(FunctionDefinition function)
            : base("function")
        {
            Function = function;
        }

        [JsonPropertyName("function")]
        public FunctionDefinition Function { get; set; }
    }



    internal sealed class JsonPropertyNameEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string value = reader.GetString() ?? throw new JsonException("Value was null.");

            foreach (var field in typeToConvert.GetFields())
            {
                var attribute = field.GetCustomAttribute<JsonPropertyNameAttribute>();
                if (attribute?.Name == value)
                {
                    return (T)Enum.Parse(typeToConvert, field.Name);
                }
            }

            throw new JsonException($"Unable to convert \"{value}\" to enum {typeToConvert}.");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<JsonPropertyNameAttribute>();

            if (attribute != null)
            {
                writer.WriteStringValue(attribute.Name);
            }
            else
            {
                writer.WriteStringValue(value.ToString());
            }
        }
    }


    [JsonConverter(typeof(JsonPropertyNameEnumConverter<ToolChoiceEnum>))]
    public enum ToolChoiceEnum
    {
        /// <summary>
        /// Auto-detect whether to call a function.
        /// </summary>
        [JsonPropertyName("auto")]
        Auto = 0,

        /// <summary>
        /// Won't call a function.
        /// </summary>
        [JsonPropertyName("none")]
        None,

        /// <summary>
        /// Force to call a function.
        /// </summary>
        [JsonPropertyName("any")]
        Any,
    }


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

        [JsonPropertyName("tools")]
        public List<FunctionTool>? Tools { get; set; }

        [JsonPropertyName("tool_choice")]
        public ToolChoiceEnum? ToolChoice { get; set; }

    }
}
