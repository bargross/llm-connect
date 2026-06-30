using LLMConnect.Exceptions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LLMConnect
{
    internal abstract class ProviderBase
    {
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

        public async Task LogAndThrow(ProviderType providerType, HttpResponseMessage response, ILogger? logger, CancellationToken cancellationToken)
        {
            var provider = providerType.ToString();

            var errorMessage = await ExtractErrorMessage(response, cancellationToken);

            var exception = new LLMConnectException(provider, errorMessage);

            logger?.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }

        public TResult? GetResponse<TResult>(string jsonString, ILogger? logger, ProviderType type)
        {
            try
            {
                return JsonSerializer.Deserialize<TResult>(jsonString);
            }
            catch (JsonException ex)
            {
                var exception = new LLMConnectException(type.ToString(), "Failed to deserialize response due to: {ex.Message}");

                logger?.LogError(exception.Provider, exception.Message);

                throw exception;
            }

        }
    }
}
