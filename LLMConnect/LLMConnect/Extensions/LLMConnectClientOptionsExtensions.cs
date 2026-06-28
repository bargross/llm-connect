using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect
{
    internal static class LLMConnectClientOptionsExtensions
    {
        internal static string InternalComputedDefaultModel(this LLMConnectClientOptions options) =>
    !string.IsNullOrWhiteSpace(options.DefaultModel) ? options.DefaultModel
        : options.Provider switch
        {
            ProviderType.Ollama => "llama3.2",
            ProviderType.Google => "gemini-2.0-flash",
            ProviderType.Anthropic => "claude-3-5-sonnet-20241022",
            ProviderType.OpenAI => "gpt-3.5-turbo",
        };
    }
}
