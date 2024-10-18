using GenAI.Server.API;
using GenAI.Server.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;
using System.Threading;

namespace GenAI.Server.Controllers
{

    [ApiController]
    [Route("v1/chat/completions")]
    public class ChatController : ControllerBase
    {


        private readonly DynamicBatchingService batcher;

        private static readonly Meter _meter = new Meter("Druid.GenAI.Server");

        private readonly Histogram<long> promptTokensHisto = _meter.CreateHistogram<long>("Chat Completion - Request Tokens", "count", "Number of prompt tokens in the request");
        private readonly Histogram<long> completionTokensHisto = _meter.CreateHistogram<long>("Chat Completion - Completion Tokens", "count", "Number of completion tokens in the response");
        private readonly Histogram<long> totalTokensHisto = _meter.CreateHistogram<long>("Chat Completion  - Total Tokens", "count", "Number of total tokens in the response");

        public ChatController(DynamicBatchingService batcher)
        {
            this.batcher = batcher;
        }

        [HttpPost]
        public async Task<ChatCompletionResponse> PostChatCompletion([FromBody] ChatCompletionRequest request, CancellationToken cancellationToken)
        {
            var response = await batcher.PredictAsync(request, cancellationToken);
            TrackRequestTokens(request, response);
            return response;
        }

        private void TrackRequestTokens(ChatCompletionRequest request, ChatCompletionResponse response)
        {
            var tags = new KeyValuePair<string, object?>("model", request.Model);


            promptTokensHisto.Record(response.Usage.PromptTokens, tags);
            completionTokensHisto.Record(response.Usage.CompletionTokens, tags);
            totalTokensHisto.Record(response.Usage.CompletionTokens, tags);
        }
    }
}
