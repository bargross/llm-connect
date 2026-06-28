using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect;

/// <summary>
/// 
/// </summary>
public class LLMClient : ILLMClient
{
    private readonly ILLMProvider _provider;

    /// <summary>
    /// user provides only options (library creates HttpClient)
    /// </summary>
    /// <param name="options"></param>
    public LLMClient(LLMClientOptions options)
        : this(options, new HttpClient())
    {
    }


    /// <summary>
    /// for the user to provide their own client already configured
    /// </summary>
    /// <param name="options">library options</param>
    /// <param name="httpClient">user defined client</param>
    public LLMClient(LLMClientOptions options, HttpClient httpClient)
        : this(new LLMProviderFactory(options, httpClient))
    {
    }

    /// <summary>
    /// for the user to provide an IHttpClientFactory
    /// </summary>
    /// <param name="options">library options</param>
    /// <param name="httpClientFactory">user defined client factory</param>
    public LLMClient(LLMClientOptions options, IHttpClientFactory httpClientFactory)
        : this(new LLMProviderFactory(options, httpClientFactory))
    {
    }

    private LLMClient(ILLMProviderFactory factory)
    {
        _provider = factory.CreateProvider();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
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
}