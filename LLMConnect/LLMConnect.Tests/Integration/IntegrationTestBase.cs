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
/// Uses a custom handler to route requests to WireMock while preserving internal URL construction.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected readonly WireMockServer _server;
    protected readonly ILoggerFactory _loggerFactory;
    protected readonly LLMConnectClient _client;
    protected readonly HttpClient _httpClient;
    protected readonly LLMConnectGeneralOptions _generalOptions;
    protected readonly LLMConnectEndpointOptions _endpointOptions;

    /// <summary>
    /// Initializes the test base with the given provider and endpoint options.
    /// </summary>
    /// <param name="provider">The provider to test.</param>
    /// <param name="endpointOptions">Endpoint options (Azure, Ollama port, etc.).</param>
    /// <param name="requiresApiKey">Whether the provider requires an API key.</param>
    protected IntegrationTestBase(
    ProviderType provider,
    LLMConnectEndpointOptions? endpointOptions = null,
    bool requiresApiKey = true)
    {
        _server = WireMockServer.Start();
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        _generalOptions = new LLMConnectGeneralOptions
        {
            Provider = provider,
            ApiKey = requiresApiKey ? "test-key" : null,
            LoggerFactory = _loggerFactory,
            MaxRetries = 2,
            Timeout = TimeSpan.FromSeconds(5),
            DefaultModel = GetDefaultModel(provider)
        };

        _endpointOptions = endpointOptions ?? new LLMConnectEndpointOptions();

        var internalBase = EndpointRegistry.GetDefaultEndpoint(
            provider,
            _endpointOptions,
            logger: _loggerFactory.CreateLogger("EndpointRegistry"));

        // Redirect handler that rewrites the request URI to point to WireMock.
        // Set its InnerHandler to an HttpClientHandler so it can send the request.
        var wiremockHandler = new WireMockRedirectHandler(_server.Urls[0], internalBase)
        {
            InnerHandler = new HttpClientHandler() // <-- FIX
        };

        var retryHandler = new RetryDelegatingHandler(
            _generalOptions.MaxRetries,
            _generalOptions.LoggerFactory?.CreateLogger<RetryDelegatingHandler>())
        {
            InnerHandler = wiremockHandler
        };

        _httpClient = new HttpClient(retryHandler)
        {
            BaseAddress = new Uri(internalBase),
            Timeout = _generalOptions.Timeout
        };

        _client = new LLMConnectClient(_generalOptions, _endpointOptions, _httpClient);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
        _httpClient.Dispose();
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

    /// <summary>
    /// Creates a default endpoint options for Azure with the given resource/deployment/version.
    /// </summary>
    protected static LLMConnectEndpointOptions CreateAzureEndpointOptions(
        string resourceName = "test-resource",
        string deploymentName = "test-deployment",
        string apiVersion = "2024-02-15-preview")
        => new LLMConnectEndpointOptions
        {
            AzureResourceName = resourceName,
            AzureDeploymentName = deploymentName,
            AzureApiVersion = apiVersion
        };

    /// <summary>
    /// Creates endpoint options for Ollama with a custom port.
    /// </summary>
    protected static LLMConnectEndpointOptions CreateOllamaEndpointOptions(int? port = null)
        => new LLMConnectEndpointOptions { OllamaPort = port };

    // ---------- Retry scenario ----------

    /// <summary>
    /// Stubs a retry scenario: the first request fails, the second succeeds.
    /// The path should match the relative path the client will request.
    /// </summary>
    protected void StubRetryScenario(
        string path,
        string failResponse,
        string successResponse,
        HttpStatusCode failStatusCode = HttpStatusCode.TooManyRequests,
        Action<IRequestBuilder>? configureRequest = null)
    {
        var scenario = "retry_scenario_" + Guid.NewGuid().ToString("N");

        // First stub (fails)
        var requestBuilder = Request.Create().WithPath(path).UsingPost();
        configureRequest?.Invoke(requestBuilder);

        _server
            .Given(requestBuilder)
            .InScenario(scenario)
            .WillSetStateTo("retried")
            .RespondWith(Response.Create()
                .WithStatusCode(failStatusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(failResponse));

        // Second stub (succeeds)
        var requestBuilder2 = Request.Create().WithPath(path).UsingPost();
        configureRequest?.Invoke(requestBuilder2);

        _server
            .Given(requestBuilder2)
            .InScenario(scenario)
            .WhenStateIs("retried")
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(successResponse));
    }

    // ---------- Private helpers ----------

    private static string GetDefaultModel(ProviderType provider)
        => provider switch
        {
            ProviderType.OpenAI => "gpt-5.5",
            ProviderType.Anthropic => "claude-sonnet-5",
            ProviderType.Google => "gemini-3.5-flash",
            ProviderType.AzureOpenAI => "gpt-4",
            ProviderType.Ollama => "qwen2.5:7b-instruct",
            _ => throw new NotSupportedException($"Provider {provider} is not supported.")
        };

    /// <summary>
    /// Delegating handler that rewrites the request URI to point to the WireMock server.
    /// It preserves the path and query, but changes the scheme, host, and port.
    /// </summary>
    private class WireMockRedirectHandler : DelegatingHandler
    {
        private readonly string _wiremockBase;
        private readonly string _targetBase;

        public WireMockRedirectHandler(string wiremockBase, string targetBase)
        {
            _wiremockBase = wiremockBase.TrimEnd('/');
            _targetBase = targetBase.TrimEnd('/');
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // The request URI is built using the internal URL (e.g., https://api.openai.com/v1/chat/completions)
            // We need to redirect it to the WireMock server while keeping the path and query.
            var originalUri = request.RequestUri!;
            var relativePath = originalUri.PathAndQuery;

            // Combine with the WireMock base URL
            var newUri = new Uri(_wiremockBase + relativePath);
            request.RequestUri = newUri;

            return base.SendAsync(request, cancellationToken);
        }
    }
}