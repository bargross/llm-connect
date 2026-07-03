using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LLMConnect
{
    internal abstract class ProviderBase(LLMConnectGeneralOptions generalOpts)
    {
        protected readonly ILogger<OpenAIProvider>? _logger = generalOpts.LoggerFactory?.CreateLogger<OpenAIProvider>();
        protected readonly IChatRequestValidator _chatRequestValidator = ChatRequestValidatorFactory.Create(generalOpts.Provider);
        protected readonly IEmbeddingRequestValidator? _embeddingRequestValidator = EmbeddingRequestValidatorFactory.Create(generalOpts.Provider);

        protected readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public async Task<string> ExtractErrorMessage(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);

                try
                {
                    using var doc = JsonDocument.Parse(errorJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("error", out var errorObj))
                    {
                        // If error is an object, try to get the "message" property
                        if (errorObj.ValueKind == JsonValueKind.Object)
                        {
                            if (errorObj.TryGetProperty("message", out var message))
                                return message.GetString() ?? $"HTTP error: {response.StatusCode}";
                        }
                        // If error is a string, use it directly
                        else if (errorObj.ValueKind == JsonValueKind.String)
                        {
                            return errorObj.GetString() ?? $"HTTP error: {response.StatusCode}";
                        }
                    }

                    // Fallback: try any top-level "message"
                    if (root.TryGetProperty("message", out var topMessage))
                        return topMessage.GetString() ?? $"HTTP error: {response.StatusCode}";

                    return $"HTTP error: {response.StatusCode} - {errorJson}";
                }
                catch (JsonException)
                {
                    return $"HTTP error: {response.StatusCode} - {errorJson}";
                }
            }
            catch (Exception ex)
            {
                return $"HTTP error: {response.StatusCode} - Failed to read error body: {ex.Message}";
            }
        }

        public async Task LogAndThrow(ProviderType providerType, HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var provider = providerType.ToString();

            var errorMessage = await ExtractErrorMessage(response, cancellationToken);

            var exception = new LLMConnectException(provider, errorMessage);

            _logger?.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }

        public async Task<TResult?> GetResponse<TResult>(HttpResponseMessage response, ProviderType type, CancellationToken cancellationToken)
        {
            try
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                return JsonSerializer.Deserialize<TResult>(json);
            }
            catch (JsonException ex)
            {
                var exception = new LLMConnectException(type.ToString(), "Failed to deserialize response due to: {ex.Message}");

                _logger?.LogError(exception.Provider, exception.Message);

                throw exception;
            }
        }

        public async IAsyncEnumerable<ChatChunk> ReadFromStreamAsync(Stream stream, LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions endpointOpts, CancellationToken cancellationToken)
        {
            if (endpointOpts.HasEndpoint && !endpointOpts.HasCustomReaderAndParser)
                throw new LLMConnectException(generalOpts.Provider.ToString(), "Custom stream event reader and parser are required when using a custom endpoint.");

            var reader = endpointOpts.CustomStreamEventReaderFactory != null
                ? endpointOpts.CustomStreamEventReaderFactory()
                : StreamReaderFactory.Create(generalOpts.Provider, generalOpts);

            var parser = endpointOpts.CustomStreamChunkParserFactory != null
                ? endpointOpts.CustomStreamChunkParserFactory()
                : StreamChunkParserFactory.Create(generalOpts.Provider, generalOpts);

            await foreach (var evt in reader.ReadEventsAsync(stream, cancellationToken))
            {
                var chunk = parser.Parse(evt);
                if (chunk != null)
                    yield return chunk;
            }
        }

        public async Task<ChatResponse> DeserializeChatResponseAsync(
            HttpResponseMessage response,
            ProviderType type,
            LLMConnectEndpointOptions options,
            CancellationToken cancellationToken)
        {
            if (!options.HasCustomChatDeserializer)
                throw new LLMConnectException(type.ToString(), "Custom response deserializer is required when using a custom endpoint.");
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                return await options.ChatResponseDeserializer(json, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError("CustomDeserializer", $"Custom response deserializer failed: {ex.Message}", ex);

                throw new LLMConnectException(
                    "CustomDeserializer",
                    $"Custom response deserializer failed: {ex.Message}",
                    ex);
            }
        }

        public async Task<EmbeddingResponse> DeserializeEmbeddingResponseAsync(
            HttpResponseMessage response,
            ProviderType type,
            LLMConnectEndpointOptions options,
            CancellationToken cancellationToken)
        {   
            if (!options.HasCustomEmbeddingDeserializer)
                throw new LLMConnectException(type.ToString(), "Custom response deserializer is required when using a custom endpoint.");

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                return await options.EmbeddingResponseDeserializer(json, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError("CustomDeserializer", $"Custom response deserializer failed: {ex.Message}", ex);

                throw new LLMConnectException(
                    "CustomDeserializer",
                    $"Custom response deserializer failed: {ex.Message}",
                    ex);
            }
        }
    }
}
