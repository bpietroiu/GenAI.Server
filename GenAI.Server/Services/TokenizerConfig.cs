using System.Text.Json.Serialization;

namespace GenAI.Server.Services
{
    class TokenizerConfig
    {
        [JsonPropertyName("eos_token")]
        public string EosToken { get; set; }
        
        [JsonPropertyName("bos_token")]
        public string BosToken { get; set; }
        
        [JsonPropertyName("pad_token")]
        public string PadToken { get; set; }

        //[JsonPropertyName("model_max_length")]
        //public int? ModelMaxLength { get; set; }


        [JsonPropertyName("chat_template")]
        public string ChatTemplate { get; set; }
        
    }
}
