using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal class LLMProviderFactory : ILLMProviderFactory
{
    private readonly HttpClient? _providedClient;
    private readonly LLMConnectClientOptions _options;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<LLMProviderFactory>? _logger;

    public LLMProviderFactory(LLMConnectClientOptions options, HttpClient httpClient)
    {
        _providedClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _logger = options.LoggerFactory?.CreateLogger<LLMProviderFactory>();

        LLMConnectOptionsValidator.Validate(options, _logger);
    }

    public LLMProviderFactory(LLMConnectClientOptions options, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _logger = options.LoggerFactory?.CreateLogger<LLMProviderFactory>();

        LLMConnectOptionsValidator.Validate(options, _logger);
    }

    public (HttpClient, ILLMProvider) CreateProvider()
    {
        var httpClient = GetOrCreateHttpClient(_options);

        return _options.Provider switch
        {
            ProviderType.OpenAI => (httpClient, new OpenAIProvider(httpClient, _options)),
            ProviderType.Anthropic => (httpClient, new AnthropicProvider(httpClient, _options)),
            ProviderType.Google => (httpClient, new GoogleProvider(httpClient, _options)),
            ProviderType.Ollama => (httpClient, new OllamaProvider(httpClient, _options)),
            _ => throw new NotSupportedException($"Provider '{_options.Provider}' is not supported.")
        };
    }

    private HttpClient GetOrCreateHttpClient(LLMConnectClientOptions options)
    {
        HttpClient client;

        if (_providedClient != null)
            client = _providedClient;
        
        else if (_httpClientFactory != null)
            client = _httpClientFactory.CreateClient("LLMConnect");
        
        else client = new HttpClient();

        return HttpClientConfigurator.ConfigureForProvider(options, client);
    }
}