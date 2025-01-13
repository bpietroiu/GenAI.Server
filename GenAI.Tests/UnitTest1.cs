using GenAI.Server.Jinja;
using Jint;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenAI.Tests
{

    class TokenizerConfig
    {
        [JsonPropertyName("chat_template")]
        public string ChatTemplate { get; set; }

        [JsonPropertyName("eos_token")]
        public string EosToken { get; set; }
        [JsonPropertyName("bos_token")]
        public string BosToken { get; set; }
    }

    [TestClass]
    public class UnitTest1
    {

        [TestMethod]
        public void TestJinjaJint()
        {
            var text = File.ReadAllText(@"D:\temp\onnx\smartix\tokenizer_config.json");
            var config = JsonSerializer.Deserialize<TokenizerConfig>(text);
            var pt = """{%- if tools %}\n    {{- '<|im_start|>system\\n' }}\n    {%- if messages[0]['role'] == 'system' %}\n        {{- messages[0]['content'] }}\n    {%- else %}\n        {{- 'You are Qwen, created by Alibaba Cloud. You are a helpful assistant.' }}\n    {%- endif %}\n    {{- \"\\n\\n# Tools\\n\\nYou may call one or more functions to assist with the user query.\\n\\nYou are provided with function signatures within <tools></tools> XML tags:\\n<tools>\" }}\n    {%- for tool in tools %}\n        {{- \"\\n\" }}\n        {{- tool | tojson }}\n    {%- endfor %}\n    {{- \"\\n</tools>\\n\\nFor each function call, return a json object with function name and arguments within <tool_call></tool_call> XML tags:\\n<tool_call>\\n{\\\"name\\\": <function-name>, \\\"arguments\\\": <args-json-object>}\\n</tool_call><|im_end|>\\n\" }}\n{%- else %}\n    {%- if messages[0]['role'] == 'system' %}\n        {{- '<|im_start|>system\\n' + messages[0]['content'] + '<|im_end|>\\n' }}\n    {%- else %}\n        {{- '<|im_start|>system\\nYou are Qwen, created by Alibaba Cloud. You are a helpful assistant.<|im_end|>\\n' }}\n    {%- endif %}\n{%- endif %}\n{%- for message in messages %}\n    {%- if (message.role == \"user\") or (message.role == \"system\" and not loop.first) or (message.role == \"assistant\" and not message.tool_calls) %}\n        {{- '<|im_start|>' + message.role + '\\n' + message.content + '<|im_end|>' + '\\n' }}\n    {%- elif message.role == \"assistant\" %}\n        {{- '<|im_start|>' + message.role }}\n        {%- if message.content %}\n            {{- '\\n' + message.content }}\n        {%- endif %}\n        {%- for tool_call in message.tool_calls %}\n            {%- if tool_call.function is defined %}\n                {%- set tool_call = tool_call.function %}\n            {%- endif %}\n            {{- '\\n<tool_call>\\n{\"name\": \"' }}\n            {{- tool_call.name }}\n            {{- '\", \"arguments\": ' }}\n            {{- tool_call.arguments | tojson }}\n            {{- '}\\n</tool_call>' }}\n        {%- endfor %}\n        {{- '<|im_end|>\\n' }}\n    {%- elif message.role == \"tool\" %}\n        {%- if (loop.index0 == 0) or (messages[loop.index0 - 1].role != \"tool\") %}\n            {{- '<|im_start|>user' }}\n        {%- endif %}\n        {{- '\\n<tool_response>\\n' }}\n        {{- message.content }}\n        {{- '\\n</tool_response>' }}\n        {%- if loop.last or (messages[loop.index0 + 1].role != \"tool\") %}\n            {{- '<|im_end|>\\n' }}\n        {%- endif %}\n    {%- endif %}\n{%- endfor %}\n{%- if add_generation_prompt %}\n    {{- '<|im_start|>assistant\\n' }}\n{%- endif %}\n                """;

            //pt = config.ChatTemplate;

            //pt = "{%- if tool_call.id == tool_call_id and not tool_call_id_seen.value -%}";

        Console.WriteLine(pt);
            Stopwatch sw = Stopwatch.StartNew();
            Template template = new Template(pt);
            string result = template.Render(new Dictionary<string, object> {
                { "bos_token", config.BosToken} ,
                { "add_generation_prompt", false},
                { "eos_token", config.EosToken} ,
                { "tools", new []{ new { type = "function", 
                    function = new { 
                        name = "get_weeather", 
                        description = "calls yahoo weather" ,
                        parameters = new object { }
                    }  
                } 
                }
                },
                { "documents", new object []{ } },
                { "messages", new []{
                    new { role = "system", content= "You are an assistant"},
                    new { role = "user", content= "Hello"},
                    new { role = "assistant", content= "How are you?"},
                    new { role = "user", content= "what is the time?"},
                }
            }
            });
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
            Console.WriteLine(result);
            
        }
    }
}