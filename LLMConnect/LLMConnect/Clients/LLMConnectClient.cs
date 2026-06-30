using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

/// <summary>
/// The default implementation of <see cref="ILLMConnectClient"/>. Routes chat
/// requests to the provider configured in <see cref="LLMConnectClientOptions"/>
/// (OpenAI, Anthropic, Google, or Ollama) through a shared, provider-agnostic API.
/// </summary>
public class LLMConnectClient : ILLMConnectClient, IDisposable
{
    private readonly ILLMProvider _provider;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly ILogger<LLMConnectClient>? _logger;

    /// <summary>
    /// user provides only options (library creates HttpClient)
    /// </summary>
    /// <param name="options"></param>
    public LLMConnectClient(LLMConnectClientOptions options)
        : this(options, new HttpClient(new RetryDelegatingHandler(options.MaxRetries, options.LoggerFactory?.CreateLogger<RetryDelegatingHandler>())
        {
            InnerHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            }
        }))
    {
        _ownsHttpClient = true;

        if (_logger is null)
            _logger = options.LoggerFactory?.CreateLogger<LLMConnectClient>();
    }


    /// <summary>
    /// for the user to provide their own client already configured
    /// </summary>
    /// <param name="options">library options</param>
    /// <param name="httpClient">user defined client</param>
    public LLMConnectClient(LLMConnectClientOptions options, HttpClient httpClient)
        : this(new LLMProviderFactory(options, httpClient))
    {
        _ownsHttpClient = false;
        _httpClient = httpClient;

        if (_logger is null)
            _logger = options.LoggerFactory?.CreateLogger<LLMConnectClient>();

        _logger?.LogWarning(
            "Using a user-supplied HttpClient. Retry logic must be configured by the caller.");
    }

    /// <summary>
    /// for the user to provide an IHttpClientFactory
    /// </summary>
    /// <param name="options">library options</param>
    /// <param name="httpClientFactory">user defined client factory</param>
    public LLMConnectClient(LLMConnectClientOptions options, IHttpClientFactory httpClientFactory)
        : this(new LLMProviderFactory(options, httpClientFactory))
    {
        _ownsHttpClient = true;

        if (_logger is null)
            _logger = options.LoggerFactory?.CreateLogger<LLMConnectClient>();
    }

    private LLMConnectClient(LLMProviderFactory factory)
    {
        var (client, provider) = factory.CreateProvider();
        
        _provider = provider;
        _httpClient = client;
    }

    /// <summary>
    /// Sends a chat completion request and returns the complete response once
    /// the model has finished generating.
    /// </summary>
    /// <param name="request">The chat request, including the conversation history and generation parameters.</param>
    /// <param name="cancellationToken">A token used to cancel the request.</param>
    /// <returns>The completed chat response, or <see langword="null"/> if no response was returned.</returns>
    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        return await _provider.ChatAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends a chat completion request and streams the response as a sequence
    /// of incremental <see cref="ChatChunk"/> values as they arrive from the provider.
    /// </summary>
    /// <param name="request">The chat request, including the conversation history and generation parameters.</param>
    /// <param name="cancellationToken">A token used to cancel the stream.</param>
    /// <returns>An asynchronous sequence of response chunks, ending with a chunk where <see cref="ChatChunk.IsComplete"/> is <see langword="true"/>.</returns>
    public IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        return _provider.StreamAsync(request, cancellationToken);
    }

    /// <summary>
    /// Releases the underlying <see cref="HttpClient"/> if this instance created
    /// and owns it. If an <see cref="HttpClient"/> or <see cref="IHttpClientFactory"/>
    /// was supplied by the caller, it is left untouched and remains the caller's
    /// responsibility to dispose.
    /// </summary>
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient?.Dispose();
        }
    }

    internal HttpClient HttpClient => _httpClient;
}