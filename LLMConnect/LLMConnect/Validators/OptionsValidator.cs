using LLMConnect.Models;
using LLMConnect.Settings;
using System;

namespace LLMConnect.Internal;

internal static class OptionsValidator
{
    public static void Validate(LLMClientOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (!Enum.IsDefined(typeof(ProviderType), options.Provider))
            throw new ArgumentException($"Invalid provider: {options.Provider}", nameof(options.Provider));

        if (options.Provider != ProviderType.Ollama && string.IsNullOrWhiteSpace(options.ApiKey))
            throw new ArgumentException($"API key is required for provider '{options.Provider}'.", nameof(options.ApiKey));

        if (!string.IsNullOrEmpty(options.Endpoint))
        {
            if (!Uri.IsWellFormedUriString(options.Endpoint, UriKind.Absolute))
                throw new ArgumentException($"Invalid endpoint URL: {options.Endpoint}.", nameof(options.Endpoint));
        }

        if (options.OllamaPort.HasValue)
        {
            if (options.Provider != ProviderType.Ollama)
                throw new ArgumentException($"OllamaPort can only be used with ProviderType.Ollama.", nameof(options.OllamaPort));

            if (options.OllamaPort.Value < 1 || options.OllamaPort.Value > 65535)
                throw new ArgumentException($"Invalid port: {options.OllamaPort.Value}. Must be between 1 and 65535.", nameof(options.OllamaPort));
        }

        if (options.Timeout <= TimeSpan.Zero)
            throw new ArgumentException($"Timeout must be greater than zero.", nameof(options.Timeout));

        if (options.MaxRetries < 0)
            throw new ArgumentException($"MaxRetries must be >= 0.", nameof(options.MaxRetries));
    }
}