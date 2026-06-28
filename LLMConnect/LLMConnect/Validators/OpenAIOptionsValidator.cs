using LLMConnect.Settings;

namespace LLMConnect;

internal class OpenAIOptionsValidator : ILLMProviderOptionsValidator
{
    public void Validate(LLMConnectClientOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new ArgumentException("API key is required for OpenAI.", nameof(options.ApiKey));

        ValidateEndpoint(options, allowEmpty: true);
        ValidateHttpsEndpoint(options);
    }

    private static void ValidateEndpoint(LLMConnectClientOptions options, bool allowEmpty)
    {
        if (string.IsNullOrEmpty(options.Endpoint))
        {
            if (!allowEmpty)
                throw new ArgumentException("Endpoint is required for OpenAI.", nameof(options.Endpoint));
            return;
        }

        if (!Uri.IsWellFormedUriString(options.Endpoint, UriKind.Absolute))
            throw new ArgumentException($"Invalid endpoint URL: {options.Endpoint}", nameof(options.Endpoint));
    }

    private static void ValidateHttpsEndpoint(LLMConnectClientOptions options)
    {
        if (string.IsNullOrEmpty(options.Endpoint))
            return;

        var uri = new Uri(options.Endpoint);
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Host != "localhost" && uri.Host != "127.0.0.1")
            throw new ArgumentException("Endpoint must use HTTPS for OpenAI.", nameof(options.Endpoint));
    }
}