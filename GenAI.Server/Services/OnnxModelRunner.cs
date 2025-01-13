using GenAI.Server.API;
using GenAI.Server.Jinja;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntimeGenAI;
using System.Text.Json;
using System.Reflection.Emit;
//using Newtonsoft.Json;

namespace GenAI.Server.Services
{


    public class ModelsRepositoryOptions
    {
        public const string ConfigKeyName = "Models";
        public string BasePath { get; set; }
    }

    public class RuntimeModelCache
    {
        public RuntimeModelCache(IOptions<ModelsRepositoryOptions> options, ILogger<RuntimeModelCache> logger)
        {
            ModelsPath = options.Value.BasePath;
            Logger = logger;
        }

        private Dictionary<string, RuntimeModel> cache = new Dictionary<string, RuntimeModel>();

        public string ModelsPath { get; }
        public ILogger<RuntimeModelCache> Logger { get; }

        public List<RuntimeModel> GetModels()
        {
            Logger?.LogInformation("Getting models");
            var models = cache.Values.ToList();
            return models;
        }

        public RuntimeModel GetModel(string modelName)
        {
            Logger?.LogInformation($"Getting model {modelName}");
            if (!cache.ContainsKey(modelName))
            {
                Logger?.LogInformation($"Loading model {modelName}");
                cache[modelName] = new RuntimeModel(Path.Combine(ModelsPath, modelName));
                Logger?.LogInformation($"Model {modelName} loaded and cached");
            }
            Logger?.LogInformation($"Returning model {modelName}");
            return cache[modelName];
        }
    }

    public class RuntimeTokenizer : IDisposable
    {
        private Tokenizer tokenizer;
        private TokenizerStream stream;
        private TokenizerConfig config;
        private Template chatTemplate;
        public RuntimeTokenizer(Model model, string modelPath)
        {
            tokenizer = new Tokenizer(model);
            stream = tokenizer.CreateStream();
            var path = Path.Combine(modelPath, "tokenizer_config.json");
            
            using var fs = File.OpenRead(path);
            config = JsonSerializer.Deserialize<TokenizerConfig>(fs);
            chatTemplate = new Template(config.ChatTemplate);
        }

        public Tokenizer Tokenizer { get { return tokenizer; } }
        public TokenizerStream Stream { get { return stream; } }

        internal TokenizerConfig Config { get => config; }

        public void Dispose()
        {
            stream.Dispose();
        }

        public string ApplyChatTemplate(ChatCompletionRequest request, bool addGenerationPrompt = true)
        {
            var context = new Dictionary<string, object> {
                { "bos_token", config.BosToken} ,
                { "add_generation_prompt", addGenerationPrompt},
                { "eos_token", config.EosToken} ,
                //{ "tools", new []{ new { name="get_weeather", description = "calls yahoo weather" } } },
                //{ "messages", new []{
                //    new { role = "system", content= "esti un asistent"},
                //    new { role = "user", content= "haide steaua"},
                //    new { role = "assistant", content= "haide dinamo"},
                //    new { role = "user", content= "cate e cesul?"}
                //      }
                //}
            };

            if (request.Tools != null && request.Tools.Any())
            {


                context["tools"] = request.Tools.ToArray();

            }
            List<object> messages = new List<object>();
            foreach (var message in request.Messages)
            {
                switch (message.Role)
                {
                    case Role.User:
                        messages.Add(new { role = "user", content = message.Content });
                        break;
                    case Role.Assistant:
                        messages.Add(new { role = "assistant", content = message.Content });
                        break;
                    case Role.System:
                        messages.Add(new { role = "system", content = message.Content });
                        break;
                }
            }
            context["messages"] = messages.ToArray();
            string result = chatTemplate.Render(context);

            return result;
        }

    }

    public class RuntimeModel
    {
        private Model model;
        private RuntimeTokenizer tokenizer;

        public string Id { get; set; }
        public long Created { get; set; }

        public RuntimeModel(string modelFullPath)
        {
            Created = DateTimeOffset.Now.ToUnixTimeSeconds();
            Id = Path.GetFileName(modelFullPath);
            model = new Model(modelFullPath);
            tokenizer = new RuntimeTokenizer(model, modelFullPath);
        }

        public Model Model { get { return model; } }
        public RuntimeTokenizer Tokenizer { get { return tokenizer; } }
    }

    public class RunResult
    {
        public int PromptTokens { get; set; }
        public int GeneratedTokens { get; set; }
        public int BatchIndex { get; set; }
        public string Output { get; set; }
        public FinishReason FinishReason { get; set; }
    }

    public class OnnxModelRunner
    {

        public OnnxModelRunner(RuntimeModelCache cache, ILogger<OnnxModelRunner> logger)
        {
            Cache = cache;
            Logger = logger;
        }

        public RuntimeModelCache Cache { get; }
        public ILogger<OnnxModelRunner> Logger { get; }

        public async IAsyncEnumerable<RunResult> RunAsync(string modelName, int max_length, double temperature, string[] batch, int[] max_lengths)
        {
            var runtimeModel = Cache.GetModel(modelName);
            if (runtimeModel == null)
            {
                throw new Exception($"Model {modelName} not found");
            }

            var model = runtimeModel.Model;
            var tokenizer = runtimeModel.Tokenizer;


            using var tokenizerStream = tokenizer.Tokenizer.CreateStream();
            using GeneratorParams generatorParams = new GeneratorParams(model);
            generatorParams.SetSearchOption("max_length", max_length);
            generatorParams.SetSearchOption("temperature", temperature);
            using var inputs = tokenizer.Tokenizer.EncodeBatch(batch);

            generatorParams.SetInputSequences(inputs);
            using var generator = new Generator(model, generatorParams);

            int generateTokenCount = 0;
            bool[] sequencesCompleted = new bool[inputs.NumSequences]; // Track which sequences have completed
            bool[] toolCalls = new bool[inputs.NumSequences]; // Track which sequences have completed
            int eosTokenId = tokenizer.Tokenizer.Encode(tokenizer.Config.EosToken)[0][0];
            int padTokenId = tokenizer.Tokenizer.Encode(tokenizer.Config.PadToken)[0][0];

            var toolCallStartTokenId = tokenizer.Tokenizer.Encode("<tool_call>")[0][0];
            var toolCallEndTokenId = tokenizer.Tokenizer.Encode("</tool_call>")[0][0];

            //Console.WriteLine($"eosTokeId: {eosTokenId}");
            //Console.WriteLine($"padTokenId: {padTokenId}");

            while (!generator.IsDone())
            {
                generateTokenCount++;
                generator.ComputeLogits();
                generator.GenerateNextToken();


                for (ulong i = 0; i < inputs.NumSequences; i++)
                {
                    if (sequencesCompleted[i]) continue; // Skip already completed sequences

                    ReadOnlySpan<int> seq = generator.GetSequence(i);
                    var token = seq[^1];
                    var decodedToken = tokenizerStream.Decode(token);

                    //Console.WriteLine($"Token: {decodedToken} [{token}]");

                    // Determine finish reason
                    FinishReason finishReason;

                    if(token == toolCallStartTokenId)
                        toolCalls[i] = true;

                    if (token == eosTokenId || token == padTokenId)
                    {
                        finishReason = toolCalls[i] ? FinishReason.FunctionCall : FinishReason.Stop;
                        sequencesCompleted[i] = true;
                    }
                    else if (seq.Length >= max_lengths[i])
                    {
                        finishReason = FinishReason.Length;
                        sequencesCompleted[i] = true;
                    }
                    else
                    {
                        finishReason = FinishReason.FunctionCall; // Placeholder, assuming additional logic can handle different reasons
                    }

                    if (sequencesCompleted[i])
                    {
                        var outputString = tokenizer.Tokenizer.Decode(seq.Slice(inputs[i].Length));

                        yield return new RunResult
                        {
                            BatchIndex = (int)i,
                            Output = outputString,
                            FinishReason = finishReason,
                            PromptTokens = inputs[i].Length,
                            GeneratedTokens = seq.Length - inputs[i].Length
                        };

                        // If all sequences are completed, exit
                        if (sequencesCompleted.All(sc => sc))
                        {
                            yield break;
                        }
                    }
                }
            }

            for (ulong i = 0; i < inputs.NumSequences; i++)
            {
                if (sequencesCompleted[i]) continue; // Skip already completed sequences

                ReadOnlySpan<int> seq = generator.GetSequence(i);
                var token = seq[^1];
                var decodedToken = tokenizerStream.Decode(token);

                //Console.WriteLine ($"Token: {decodedToken} [{token}]");

                // Determine finish reason
                FinishReason finishReason;

                if (seq.Length >= max_lengths[i])
                {
                    finishReason = FinishReason.Length;
                    sequencesCompleted[i] = true;
                }
                else
                {
                    finishReason = toolCalls[i] ? FinishReason.FunctionCall : FinishReason.Stop;
                    sequencesCompleted[i] = true;
                }

                if (sequencesCompleted[i])
                {
                    var outputString = tokenizer.Tokenizer.Decode(seq.Slice(inputs[i].Length));

                    yield return new RunResult
                    {
                        BatchIndex = (int)i,
                        Output = outputString,
                        FinishReason = finishReason,
                        PromptTokens = inputs[i].Length,
                        GeneratedTokens = seq.Length - inputs[i].Length
                    };

                    // If all sequences are completed, exit
                    if (sequencesCompleted.All(sc => sc))
                    {
                        yield break;
                    }
                }
            }

        }
    }
}
