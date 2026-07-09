using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace LLMConnect.Tests.Integration;

/// <summary>
/// Base class for integration tests using WireMock.
/// Provides server setup, client creation, and common stub helpers.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected readonly WireMockServer _server;
    protected readonly ILoggerFactory _loggerFactory;
    protected readonly LLMConnectClient _client;

    protected IntegrationTestBase(ProviderType provider, string endpointPath, bool requiresApiKey = true)
    {
        _server = WireMockServer.Start();
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var baseAddress = _server.Urls[0] + "/";

        var options = new LLMConnectClientOptions
        {
            Provider = provider,
            ApiKey = requiresApiKey ? "test-key" : null,
            BaseUrl = baseAddress,
            LoggerFactory = _loggerFactory,
            MaxRetries = 2,
            Timeout = TimeSpan.FromSeconds(5)
        };

        var httpClient = new HttpClient(new RetryDelegatingHandler(options.MaxRetries, options.LoggerFactory?.CreateLogger<RetryDelegatingHandler>())
        {
            InnerHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            }
        })
        {
            BaseAddress = new Uri(baseAddress)
        };

        _client = new LLMConnectClient(options, httpClient);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
        _loggerFactory.Dispose();
    }

    // ---------- Common helpers ----------

    protected ChatRequest CreateChatRequest(string userMessage = "Hello")
        => new ChatRequest
        {
            Messages = new List<Message> { new UserMessage(userMessage) }
        };

    protected EmbeddingRequest CreateEmbeddingRequest(string text = "Hello")
        => new EmbeddingRequest { Text = text };

    // ---------- Retry scenario ----------
    protected void StubRetryScenario(string path, string failResponse, string successResponse,
        HttpStatusCode failStatusCode = HttpStatusCode.TooManyRequests)
    {
        var scenario = "retry_scenario_" + Guid.NewGuid().ToString("N");

        _server
            .Given(Request.Create().WithPath(path).UsingPost())
            .InScenario(scenario)
            .WillSetStateTo("retried")
            .RespondWith(Response.Create()
                .WithStatusCode(failStatusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(failResponse));

        _server
            .Given(Request.Create().WithPath(path).UsingPost())
            .InScenario(scenario)
            .WhenStateIs("retried")
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(successResponse));
    }
}