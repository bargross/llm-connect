using LLMConnect.Models;

namespace LLMConnect
{
    internal static class ChatRequestValidatorFactory
    {
        public static IChatRequestValidator Create(ProviderType provider)
        {
            return provider switch
            {
                ProviderType.OpenAI => new OpenAIChatRequestValidator(),
                ProviderType.Anthropic => new AnthropicChatRequestValidator(),
                ProviderType.Google => new GoogleChatRequestValidator(),
                ProviderType.Ollama => new OllamaChatRequestValidator(),
                _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
            };
        }
    }
}
