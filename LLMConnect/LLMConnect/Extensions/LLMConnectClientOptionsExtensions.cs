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
                    ProviderType.Ollama => "llama3.2",
                    ProviderType.Google => "gemini-2.0-flash",
                    ProviderType.Anthropic => "claude-3-5-sonnet-20241022",
                    ProviderType.OpenAI => "gpt-3.5-turbo",
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
                LoggerFactory = options.LoggerFactory
            };
        }

        internal static LLMConnectEndpointOptions ToEndpointOptions(this LLMConnectClientOptions options)
        {
            if (options == null)
                return new LLMConnectEndpointOptions();

            return new LLMConnectEndpointOptions
            {
                Endpoint = options.Endpoint,
                OllamaPort = options.OllamaPort,
                ChatResponseDeserializer = options.ChatResponseDeserializer,
                EmbeddingResponseDeserializer = options.EmbeddingResponseDeserializer,
                CustomStreamEventReaderFactory = options.CustomStreamEventReaderFactory,
                CustomStreamChunkParserFactory = options.CustomStreamChunkParserFactory,
                ExtraOptions = options.ExtraOptions
            };
        }
    }
}
