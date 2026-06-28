using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect;

internal class OllamaOptionsValidator : ILLMProviderOptionsValidator
{
    public void Validate(LLMConnectClientOptions options)
    {
        // Ollama does not require an API key

        if (options.OllamaPort.HasValue)
        {
            if (options.OllamaPort.Value < 1 || options.OllamaPort.Value > 65535)
                throw new ArgumentException($"Invalid port: {options.OllamaPort.Value}. Must be between 1 and 65535.", nameof(options.OllamaPort));
        }

        ValidateEndpoint(options, allowEmpty: true);

        if (string.IsNullOrEmpty(options.Endpoint))
        {
            var defaultEndpoint = EndpointRegistry.GetDefaultEndpoint(ProviderType.Ollama);
            if (!defaultEndpoint.Contains("{port}"))
                throw new InvalidOperationException(
                    $"The default endpoint for Ollama is invalid. It must contain '{{port}}' placeholder. Current endpoint: {defaultEndpoint}");
        }
    }

    private static void ValidateEndpoint(LLMConnectClientOptions options, bool allowEmpty)
    {
        if (string.IsNullOrEmpty(options.Endpoint))
        {
            if (!allowEmpty)
                throw new ArgumentException("Endpoint is required for Ollama.", nameof(options.Endpoint));
            return;
        }

        if (!Uri.IsWellFormedUriString(options.Endpoint, UriKind.Absolute))
            throw new ArgumentException($"Invalid endpoint URL: {options.Endpoint}", nameof(options.Endpoint));
    }
}