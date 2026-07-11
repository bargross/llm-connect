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
        protected readonly LLMConnectGeneralOptions _generalOpts = generalOpts;
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

        public async Task LogAndThrow(
            ProviderType? providerType, 
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            var provider = providerType?.ToString() ?? "unknown";

            var errorMessage = await ExtractErrorMessage(response, cancellationToken);

            var exception = new LLMConnectException(provider, errorMessage);

            _logger?.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }

        protected async Task<TResponse?> DeserializeResponseAsync<TProviderResponse, TResponse>(
            HttpResponseMessage response,
            ProviderType? provider,
            Func<TProviderResponse?, TResponse?> toChatResponse,
            CancellationToken cancellationToken)
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // Otherwise, use the standard provider-specific deserialization
            var providerResponse = JsonSerializer.Deserialize<TProviderResponse>(json);

            var chatResponse = toChatResponse.Invoke(providerResponse);
            if (chatResponse == null)
                throw new LLMConnectException(provider?.ToString() ?? "unknown", "Failed to deserialize response.");

            return chatResponse;
        }

        protected string GetUrlRelativePath(
            LLMConnectEndpointOptions options,
            QueryType type,
            bool isStreaming,
            string? model = null,
            ILogger? logger = null)
        {
            if (_generalOpts.Provider == ProviderType.Google && string.IsNullOrWhiteSpace(model))
                throw new LLMConnectException(_generalOpts.Provider?.ToString() ?? "ProviderBase", "Model must be specified for Google provider.");

            var queryParams = EndpointRegistry.GetEndpointParams(
                _generalOpts.Provider.Value,
                type,
                isStreaming,
                model,
                logger);

            return queryParams;
        }
    }
}
