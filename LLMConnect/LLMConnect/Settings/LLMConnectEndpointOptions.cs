using LLMConnect.Models;
using LLMConnect.Streams;

namespace LLMConnect.Settings
{
    /// <summary>
    /// Configuration options for connecting to a specific LLM provider endpoint, including custom deserialization and streaming behavior.
    /// </summary>
    public class LLMConnectEndpointOptions
    {
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

        /// <summary>
        /// Optional custom response deserializer for provider-specific or custom endpoints.
        /// If provided, this delegate is used to deserialize the HTTP response content
        /// into a ChatResponse object, bypassing the built-in provider-specific deserialization.
        /// </summary>
        public Func<string, CancellationToken, Task<ChatResponse?>>? ChatResponseDeserializer { get; set; }

        /// <summary>
        /// Optional custom embedding response deserializer for provider-specific or custom endpoints.
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

        internal bool HasCustomChatDeserializer => ChatResponseDeserializer is not null;

        internal bool HasCustomEmbeddingDeserializer => EmbeddingResponseDeserializer is not null;

        internal bool HasEndpoint => !string.IsNullOrWhiteSpace(Endpoint);

        internal bool HasCustomReaderAndParser => CustomStreamEventReaderFactory is not null && CustomStreamChunkParserFactory is not null;
    }
}
