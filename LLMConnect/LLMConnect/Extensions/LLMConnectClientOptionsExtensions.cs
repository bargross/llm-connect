using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect
{
    internal static class LLMConnectClientOptionsExtensions
    {
        internal static string InternalComputedDefaultModel(this LLMConnectGeneralOptions options, string? model = null) =>
            !string.IsNullOrWhiteSpace(model) ? model
                : !string.IsNullOrWhiteSpace(options.DefaultModel) ? options.DefaultModel
                : options.Provider switch
                {
                    ProviderType.Ollama => "qwen2.5:7b-instruct",
                    ProviderType.Google => "gemini-3.5-flash",
                    ProviderType.Anthropic => "claude-sonnet-5",
                    ProviderType.OpenAI => "gpt-5.5",
                    ProviderType.AzureOpenAI => "gpt-4",
                    _ => throw new NotSupportedException($"Provider '{options.Provider}' is not supported.")
                };


        internal static LLMConnectGeneralOptions ToGeneralOptions(this LLMConnectClientOptions options)
        {
            if (options == null)
                return new LLMConnectGeneralOptions();

            return new LLMConnectGeneralOptions
            {
                Provider = options.Provider,
                ApiKey = options.ApiKey,
                DefaultModel = options.DefaultModel,
                Timeout = options.Timeout,
                MaxRetries = options.MaxRetries,
                LoggerFactory = options.LoggerFactory,
                
            };
        }

        internal static LLMConnectEndpointOptions ToEndpointOptions(this LLMConnectClientOptions options)
        {
            if (options == null)
                return new LLMConnectEndpointOptions();

            return new LLMConnectEndpointOptions
            {
                AzureApiVersion = options.AzureApiVersion,
                AzureDeploymentName = options.AzureDeploymentName,
                AzureResourceName = options.AzureResourceName,
                OllamaPort = options.OllamaPort,
                ExtraOptions = options.ExtraOptions
            };
        }
    }
}
