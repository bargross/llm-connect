using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

/// <summary>
/// 
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
        : this(options, new HttpClient(new RetryDelegatingHandler(options.MaxRetries, options.LoggerFactory?.CreateLogger<RetryDelegatingHandler>())))
    {
        _ownsHttpClient = true;

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
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        return await _provider.ChatAsync(request, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        return _provider.StreamAsync(request, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient?.Dispose();
        }
    }
}