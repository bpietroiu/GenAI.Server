using GenAI.Server.Controllers;
using GenAI.Server.Services;
using Microsoft.ML.OnnxRuntimeGenAI;
using Microsoft.VisualBasic.FileIO;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text.Json.Serialization;

namespace GenAI.Server
{


    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateSlimBuilder(args);

            builder.Services.Configure<ModelsRepositoryOptions>(
                builder.Configuration.GetSection(ModelsRepositoryOptions.ConfigKeyName));

            builder.Services.AddOptions<DynamicBatchingOptions>()
                .Bind(builder.Configuration.GetSection(DynamicBatchingOptions.DynamicBatching));

            builder.Services.AddSingleton<DynamicBatchingService>();
            builder.Services.AddHostedService<DynamicBatchingService>(provider => provider.GetService<DynamicBatchingService>());

            builder.Services.AddSingleton<RuntimeModelCache>();
            builder.Services.AddSingleton<OnnxModelRunner>();

            // Add MVC services to the DI container
            builder.Services.AddControllers();


            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.SerializerOptions.WriteIndented = true;
            });

            var internalApplicationName = "genai_server";
            if (builder.Configuration["UseOpenTelemetry"] == "1" )
            {
                // Add OpenTelemetry tracing and metrics
                builder.Services.AddOpenTelemetry()
                    .WithTracing(tracerProviderBuilder =>
                    {
                        var tpb = tracerProviderBuilder
                            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(internalApplicationName, serviceInstanceId: Environment.MachineName))
                            .AddSource(internalApplicationName) // Add a source to the tracer
                            .AddAspNetCoreInstrumentation(options => options.Filter = httpContext => !httpContext.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) &&
                                                           !httpContext.Request.Path.Value.StartsWith("/healthz", StringComparison.InvariantCultureIgnoreCase) &&
                                                           !httpContext.Request.Path.Value.StartsWith("/healthcheck", StringComparison.InvariantCultureIgnoreCase) &&
                                                           !httpContext.Request.Path.Value.StartsWith("/readycheck", StringComparison.InvariantCultureIgnoreCase)) // Instrument ASP.NET Core
                            .AddHttpClientInstrumentation(); // Instrument outgoing HTTP calls
        
                        if(Uri.TryCreate(builder.Configuration["OtelEndpoint"], new UriCreationOptions(), out var otelEndpoint))
                            tpb.AddOtlpExporter(otlpOptions => otlpOptions.Endpoint = otelEndpoint);
                        else
                            tpb.AddConsoleExporter();
                    })
                    .WithMetrics(metricsProviderBuilder =>
                    {
                        var mpb = metricsProviderBuilder
                            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(internalApplicationName, serviceInstanceId: Environment.MachineName))
                            .AddAspNetCoreInstrumentation() // Instrument ASP.NET Core metrics
                            .AddMeter(internalApplicationName);
                        if (Uri.TryCreate(builder.Configuration["OtelEndpoint"], new UriCreationOptions(), out var otelEndpoint))
                            mpb.AddOtlpExporter(otlpOptions => otlpOptions.Endpoint = otelEndpoint);
                        else
                            mpb.AddConsoleExporter();                            

                    });
            }
            


            var app = builder.Build();


            app.MapControllers();

            app.Run();
        }
    }

}
