using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect;

internal static class LLMConnectOptionsValidator
{
    private static readonly Dictionary<ProviderType, ILLMProviderOptionsValidator> _validators = new()
    {
        { ProviderType.OpenAI, new OpenAIOptionsValidator() },
        { ProviderType.Anthropic, new AnthropicOptionsValidator() },
        { ProviderType.Google, new GoogleOptionsValidator() },
        { ProviderType.Ollama, new OllamaOptionsValidator() }
    };

    public static void Validate(LLMConnectClientOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (!Enum.IsDefined(typeof(ProviderType), options.Provider))
            throw new ArgumentException($"Invalid provider: {options.Provider}", nameof(options.Provider));

        if (options.Timeout <= TimeSpan.Zero)
            throw new ArgumentException("Timeout must be greater than zero.", nameof(options.Timeout));

        if (options.MaxRetries < 0)
            throw new ArgumentException("MaxRetries must be >= 0.", nameof(options.MaxRetries));

        if (!string.IsNullOrEmpty(options.DefaultModel) && options.DefaultModel.Length > 100)
            throw new ArgumentException("DefaultModel cannot exceed 100 characters.", nameof(options.DefaultModel));

        // Dispatch to the provider-specific validator
        if (_validators.TryGetValue(options.Provider, out var validator))
            validator.Validate(options);
        else
            throw new NotSupportedException($"Provider '{options.Provider}' is not supported.");
    }
}