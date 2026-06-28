using LLMConnect;
using LLMConnect.Models;

internal static class OptionsValidatorFactory
{
    public static ILLMProviderOptionsValidator Create(ProviderType provider)
    {
        return provider switch
        {
            ProviderType.OpenAI => new OpenAIOptionsValidator(),
            ProviderType.Anthropic => new AnthropicOptionsValidator(),
            ProviderType.Google => new GoogleOptionsValidator(),
            ProviderType.Ollama => new OllamaOptionsValidator(),
            _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
        };
    }
}