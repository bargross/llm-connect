using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect;

internal class LLMProviderFactory : ILLMProviderFactory
{
    private readonly HttpClient? _providedClient;
    private readonly LLMClientOptions _options;
    private readonly IHttpClientFactory? _httpClientFactory;

    public LLMProviderFactory(LLMClientOptions options, HttpClient httpClient)
    {
        _providedClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        OptionsValidator.Validate(options);
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public LLMProviderFactory(LLMClientOptions options, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

        OptionsValidator.Validate(options);
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public LLMProviderFactory() { }

    public ILLMProvider CreateProvider()
    {
        var httpClient = GetOrCreateHttpClient(_options);

        return _options.Provider switch
        {
            ProviderType.OpenAI => new OpenAIProvider(httpClient, _options),
            ProviderType.Anthropic => new AnthropicProvider(httpClient, _options),
            ProviderType.Google => new GoogleProvider(httpClient, _options),
            ProviderType.Ollama => new OllamaProvider(httpClient, _options),
            _ => throw new NotSupportedException($"Provider '{_options.Provider}' is not supported.")
        };
    }

    private HttpClient GetOrCreateHttpClient(LLMClientOptions options)
    {
        HttpClient client;

        if (_providedClient != null)
        {
            client = _providedClient;
        }
        else if (_httpClientFactory != null)
        {
            client = _httpClientFactory.CreateClient("LLMConnect");
        }
        else
        {
            client = new HttpClient();
        }

        return HttpClientConfigurator.ConfigureForProvider(options, client);
    }
}