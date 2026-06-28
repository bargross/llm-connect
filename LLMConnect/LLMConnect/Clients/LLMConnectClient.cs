using LLMConnect.Internal;
using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect;

/// <summary>
/// 
/// </summary>
public class LLMConnectClient : ILLMConnectClient, IDisposable
{
    private readonly ILLMProvider _provider;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    /// <summary>
    /// user provides only options (library creates HttpClient)
    /// </summary>
    /// <param name="options"></param>
    public LLMConnectClient(LLMConnectClientOptions options)
        : this(options, new HttpClient(new RetryDelegatingHandler(options.MaxRetries, new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) })))
    {
        _ownsHttpClient = true;
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
    }

    private LLMConnectClient(ILLMProviderFactory factory)
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