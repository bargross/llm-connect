using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LLMConnect
{
    internal abstract class ProviderBase<TProvider>(LLMConnectGeneralOptions generalOpts)
    {
        protected readonly ILogger<TProvider>? _logger = generalOpts.LoggerFactory?.CreateLogger<TProvider>();
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

        public async IAsyncEnumerable<ChatChunk> ReadFromStreamAsync(Stream stream, LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions endpointOpts, CancellationToken cancellationToken)
        {
            var reader = endpointOpts.HasEndpoint && endpointOpts.HasCustomReaderAndParser
                ? endpointOpts.CustomStreamEventReaderFactory()
                : StreamReaderFactory.Create(generalOpts.Provider, generalOpts);

            var parser = endpointOpts.HasEndpoint && endpointOpts.HasCustomReaderAndParser
                ? endpointOpts.CustomStreamChunkParserFactory()
                : StreamChunkParserFactory.Create(generalOpts.Provider, generalOpts);

            await foreach (var evt in reader.ReadEventsAsync(stream, cancellationToken))
            {
                var chunk = parser.Parse(evt);
                if (chunk != null)
                    yield return chunk;
            }
        }

        protected async Task<TResponse?> DeserializeResponseAsync<TProviderResponse, TResponse>(
            HttpResponseMessage response,
            ProviderType provider,
            Func<bool> customDeserializerEvaluator,
            Func<string, Task<TResponse?>> jsonStringToResponse,
            Func<TProviderResponse, TResponse?> toChatResponse,
            CancellationToken cancellationToken)
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // If a custom deserializer is provided, use it
            if (customDeserializerEvaluator())
            {
                try
                {
                    return await jsonStringToResponse(json);
                }
                catch (Exception ex)
                {
                    throw new LLMConnectException("CustomDeserializer", $"Chat deserializer failed: {ex.Message}", ex);
                }
            }

            // Otherwise, use the standard provider-specific deserialization
            var providerResponse = JsonSerializer.Deserialize<TProviderResponse>(json);

            var chatResponse = toChatResponse.Invoke(providerResponse);
            if (chatResponse == null)
                throw new LLMConnectException(provider.ToString(), "Failed to deserialize response.");

            return chatResponse;
        }
    }
}
