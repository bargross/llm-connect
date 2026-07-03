using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect.Settings
{
    /// <summary>
    /// Represents general configuration options for the LLMConnect client.
    /// </summary>
    public class LLMConnectGeneralOptions
    {
        /// <summary>The LLM provider to target.</summary>
        public ProviderType Provider { get; set; } = ProviderType.OpenAI;

        /// <summary>The API key for the configured provider. Not required when <see cref="Provider"/> is <see cref="ProviderType.Ollama"/>.</summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>The model used for a request when <see cref="ChatRequest.Model"/> is not set.</summary>
        public string? DefaultModel { get; set; }

        /// <summary>The per-request HTTP timeout.</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>The maximum number of retry attempts for transient failures. Must be zero or greater; <c>0</c> disables retries.</summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Optional logger factory. If provided, logs will be emitted.
        /// </summary>
        public ILoggerFactory? LoggerFactory { get; set; }
    }
}
