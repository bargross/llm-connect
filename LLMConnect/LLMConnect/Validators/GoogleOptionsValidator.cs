using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect;

internal class GoogleOptionsValidator : ILLMProviderOptionsValidator
{
    public void Validate(LLMConnectClientOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new ArgumentException("API key is required for Google.", nameof(options.ApiKey));

        ValidateEndpoint(options, allowEmpty: true);
        ValidateHttpsEndpoint(options);

        // Ensure the default endpoint has the required placeholders
        if (string.IsNullOrEmpty(options.Endpoint))
        {
            var defaultEndpoint = EndpointRegistry.GetDefaultEndpoint(ProviderType.Google);
            if (!defaultEndpoint.Contains("{model}") || !defaultEndpoint.Contains("{key}"))
                throw new InvalidOperationException(
                    $"The default endpoint for Google is invalid. It must contain '{{model}}' and '{{key}}' placeholders. Current endpoint: {defaultEndpoint}");
        }
    }

    private static void ValidateEndpoint(LLMConnectClientOptions options, bool allowEmpty)
    {
        if (string.IsNullOrEmpty(options.Endpoint))
        {
            if (!allowEmpty)
                throw new ArgumentException("Endpoint is required for Google.", nameof(options.Endpoint));
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
            throw new ArgumentException("Endpoint must use HTTPS for Google.", nameof(options.Endpoint));
    }
}