using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect.Settings;

/// <summary>
/// Configuration options for an <see cref="LLMConnect.LLMConnectClient"/> instance,
/// including which provider to use, credentials, and request behavior.
/// </summary>
public class LLMConnectClientOptions
{
    /// <summary>The LLM provider to target.</summary>
    public ProviderType Provider { get; set; } = ProviderType.OpenAI;

    /// <summary>The API key for the configured provider. Not required when <see cref="Provider"/> is <see cref="ProviderType.Ollama"/>.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>The model used for a request when <see cref="ChatRequest.Model"/> is not set.</summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// An optional override for the provider's default endpoint URL. Takes
    /// precedence over <see cref="OllamaPort"/> when both are set.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// The port a local Ollama server is listening on. Defaults to <c>11434</c>
    /// if not set. Ignored if <see cref="Endpoint"/> is set.
    /// </summary>
    public int? OllamaPort { get; set; }

    /// <summary>The per-request HTTP timeout.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>The maximum number of retry attempts for transient failures. Must be zero or greater; <c>0</c> disables retries.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Optional logger factory. If provided, logs will be emitted.
    /// </summary>
    public ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>
    /// Optional custom response deserializer for provider-specific or custom endpoints.
    /// If provided, this delegate is used to deserialize the HTTP response content
    /// into a ChatResponse object, bypassing the built-in provider-specific deserialization.
    /// </summary>
    public Func<string, CancellationToken, Task<ChatResponse?>>? ChatResponseDeserializer { get; set; }

    /// <summary>
    /// Optional custom response deserializer for provider-specific or custom endpoints.
    /// If provided, this delegate is used to deserialize the HTTP response content
    /// into an EmbeddingResponse object, bypassing the built-in provider-specific deserialization.
    /// </summary>
    public Func<string, CancellationToken, Task<EmbeddingResponse?>>? EmbeddingResponseDeserializer { get; set; }

    /// <summary>
    /// Optional custom stream event reader for custom endpoints.
    /// If provided, this is used instead of the built-in reader for the provider.
    /// </summary>
    public Func<IStreamEventReader>? CustomStreamEventReaderFactory { get; set; }

    /// <summary>
    /// Optional custom stream chunk parser for custom endpoints.
    /// If provided, this is used instead of the built-in parser for the provider.
    /// </summary>
    public Func<IStreamChunkParser>? CustomStreamChunkParserFactory { get; set; }

    /// <summary>Reserved for future provider-specific configuration.</summary>
    public Dictionary<string, object>? ExtraOptions { get; set; }
}