using GenAI.Server.API;
using Microsoft.AspNetCore.Components.Forms;
using System.Threading.Channels;

namespace GenAI.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    public class DynamicBatchingOptions
    {

        public const string DynamicBatching = "DynamicBatching";
        public int MaxBatchSize { get; set; }
        public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromMilliseconds(20);
    }



    public class DynamicBatchingService : IHostedService
    {
        
        public class Request
        {
            public ChatCompletionRequest Input { get; }
            public TaskCompletionSource<ChatCompletionResponse> TaskCompletionSource { get; }

            public Request(ChatCompletionRequest input)
            {
                Input = input;
                TaskCompletionSource = new TaskCompletionSource<ChatCompletionResponse>();
            }
        }




        private readonly Channel<Request> _requestChannel;
        private readonly int _maxBatchSize;
        private readonly TimeSpan _maxWaitTime;
        private readonly OnnxModelRunner _onnxModelRunner;
        private CancellationTokenSource _cts;
        private Task _batchProcessorTask;

        public DynamicBatchingService(OnnxModelRunner runner, IOptions<DynamicBatchingOptions> config)
        {
            _maxBatchSize = config.Value.MaxBatchSize;
            _maxWaitTime = config.Value.MaxWaitTime;
            _requestChannel = Channel.CreateUnbounded<Request>();
            _onnxModelRunner = runner;
        }


        // Method to be called for each incoming request with a cancellation token
        public async Task<ChatCompletionResponse> PredictAsync(ChatCompletionRequest input, CancellationToken cancellationToken)
        {
            var request = new Request(input);

            // Attempt to write the request to the channel
            if (_requestChannel.Writer.TryWrite(request))
            {
                // Register cancellation in case the caller cancels the task
                using (cancellationToken.Register(() => request.TaskCompletionSource.TrySetCanceled(cancellationToken)))
                {
                    return await request.TaskCompletionSource.Task;
                }
            }
            else
            {
                throw new InvalidOperationException("Service is shutting down and not accepting new requests.");
            }
        }

        // Called by the ASP.NET Core framework to start the service
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();
            _batchProcessorTask = Task.Run(() => ProcessBatches(_cts.Token), cancellationToken);
            return Task.CompletedTask;
        }

        // Called by the ASP.NET Core framework to stop the service
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Cancel the processing loop
            _cts.Cancel();

            // Complete the channel to indicate no more requests will be accepted
            _requestChannel.Writer.Complete();

            // Wait for the batch processor task to finish
            await _batchProcessorTask;
        }

        // Batch processor
        private async Task ProcessBatches(CancellationToken cancellationToken)
        {
            // Dictionary to store batches by (Model, Temperature) combination
            var batches = new Dictionary<(string model, float temperature), List<Request>>();

            try
            {
                while (await _requestChannel.Reader.WaitToReadAsync(cancellationToken))
                {
                    try
                    {
                        // Process requests from the channel
                        while (_requestChannel.Reader.TryRead(out var request))
                        {
                            // Create a key based on Model and Temperature
                            var key = (request.Input.Model, request.Input.Temperature);

                            // Initialize the batch for the specific (Model, Temperature) combination if it doesn't exist
                            if (!batches.ContainsKey(key))
                            {
                                batches[key] = new List<Request>();
                            }

                            // Add the request to the appropriate batch
                            batches[key].Add(request);

                            // Process the batch if it reaches the max batch size
                            if (batches[key].Count >= _maxBatchSize)
                            {
                                await ProcessBatch(batches[key], key, cancellationToken);
                                batches[key].Clear(); // Clear the batch after processing
                            }
                        }

                        // Process all batches after waiting for max wait time
                        var delayTask = Task.Delay(_maxWaitTime, cancellationToken);  // Task to manage timeout
                        var readTask = _requestChannel.Reader.WaitToReadAsync(cancellationToken).AsTask();

                        var completedTask = await Task.WhenAny(readTask, delayTask);

                        // If the timeout expires, process all remaining batches
                        if (completedTask == delayTask)
                        {
                            foreach (var batch in batches.Where(b => b.Value.Count > 0).ToList())
                            {
                                await ProcessBatch(batch.Value, batch.Key, cancellationToken);
                                batch.Value.Clear();  // Clear the batch after processing
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Handle graceful shutdown
                        foreach (var batch in batches)
                        {
                            foreach (var req in batch.Value)
                            {
                                req.TaskCompletionSource.TrySetCanceled();
                            }
                        }
                        break; // Exit the processing loop on cancellation
                    }
                    catch (Exception ex)
                    {
                        // Handle errors for each request in the batch
                        foreach (var batch in batches)
                        {
                            foreach (var req in batch.Value)
                            {
                                req.TaskCompletionSource.SetException(ex);
                            }
                        }
                        batches.Clear(); // Clear all batches after error
                    }
                }
            }
            finally
            {
                // Final cleanup: cancel any remaining requests
                foreach (var batch in batches)
                {
                    foreach (var request in batch.Value)
                    {
                        request.TaskCompletionSource.TrySetCanceled();
                    }
                }
            }
        }

        private async Task ProcessBatch(List<Request> batch, (string model, float temperature) key, CancellationToken cancellationToken)
        {
            try
            {
                // Extract the inputs for the batch
                var inputs = batch.Select(r => r.Input).ToList();
                var model = _onnxModelRunner.Cache.GetModel(key.model);
                var inputStrings = inputs.Select(x => model.Tokenizer.ApplyChatTemplate(x));

                int? maxLen = batch.Max(x => x.Input.MaxTokens);

                int[] max_lengths = batch.Select(r=>r.Input.MaxTokens.Value).ToArray();
                // Call the ONNX model for the specific batch
                var predictions = _onnxModelRunner.RunAsync(key.model, maxLen.Value, key.temperature, inputStrings.ToArray(), max_lengths);  // Modify this method to use model and temperature

                await foreach (var result in predictions)
                {
                    var response = new ChatCompletionResponse
                    {
                        Id = Guid.NewGuid().ToString(),
                        Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        Choices = new[] {
                    new Choice
                    {
                        Index = 0,
                        Message = new Message
                        {
                            Role = Role.Assistant,
                            Content = result.Output
                        },
                        FinishReason = result.FinishReason
                    }
                }.ToList(),
                        Usage = new CompletionUsage
                        {
                            PromptTokens = result.PromptTokens,
                            CompletionTokens = result.GeneratedTokens,
                            TotalTokens = result.PromptTokens + result.GeneratedTokens
                        }
                    };

                    // Send the response back to the corresponding request
                    batch[result.BatchIndex].TaskCompletionSource.SetResult(response);
                }

            }
            catch (Exception ex)
            {
                // Handle errors for each request in the batch
                foreach (var request in batch)
                {
                    request.TaskCompletionSource.SetException(ex);
                }
            }
        }
    }

}
