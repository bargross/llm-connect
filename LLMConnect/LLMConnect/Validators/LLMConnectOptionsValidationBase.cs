using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect;

internal abstract class LLMConnectOptionsValidationBase
{
    public virtual void Validate(LLMConnectClientOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        ValidateApiKey(options);

        if (options.Timeout <= TimeSpan.Zero)
            throw new ArgumentException($"Timeout must be greater than zero.", nameof(options.Timeout));

        if (options.MaxRetries < 0)
            throw new ArgumentException($"MaxRetries must be >= 0.", nameof(options.MaxRetries));

        if (!string.IsNullOrWhiteSpace(options.DefaultModel) && options.DefaultModel.Length > 100)
            throw new ArgumentException($"DefaultModel cannot exceed 100 characters.", nameof(options.DefaultModel));

        ValidateEndpoint(options);

        ValidateProviderSpecific(options);
    }

    protected virtual void ValidateEndpoint(LLMConnectClientOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
            return;

        if (!Uri.IsWellFormedUriString(options.Endpoint, UriKind.Absolute))
            throw new ArgumentException($"Invalid endpoint URL: {options.Endpoint}", nameof(options.Endpoint));

        var uri = new Uri(options.Endpoint);
        if (options.Provider != ProviderType.Ollama &&
            uri.Scheme != Uri.UriSchemeHttps &&
            uri.Host != "localhost" &&
            uri.Host != "127.0.0.1")
        {
            throw new ArgumentException($"Endpoint must use HTTPS for provider '{options.Provider.ToString()}'.", nameof(options.Endpoint));
        }
    }

    protected virtual void ValidateApiKey(LLMConnectClientOptions options)
    {
        switch (options.Provider)
        {
            case ProviderType.Ollama: break;
            case ProviderType.Anthropic:
            case ProviderType.OpenAI:
            case ProviderType.Google:
                if (string.IsNullOrWhiteSpace(options.ApiKey))
                    throw new ArgumentException($"Missing api key for provider {options.Provider.ToString()}");

                break;
        }
    }

    protected abstract void ValidateProviderSpecific(LLMConnectClientOptions options);
}