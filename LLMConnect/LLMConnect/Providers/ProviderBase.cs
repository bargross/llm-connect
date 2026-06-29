using LLMConnect.Exceptions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
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

                // Try to extract "message" from common error formats (OpenAI, Anthropic, Google, etc.)
                try
                {
                    using var doc = JsonDocument.Parse(errorJson);
                    var root = doc.RootElement;

                    // Try common error paths
                    if (root.TryGetProperty("error", out var errorObj))
                    {
                        // OpenAI: { "error": { "message": "..." } }
                        if (errorObj.TryGetProperty("message", out var message))
                            return message.GetString() ?? $"HTTP error: {response.StatusCode}";

                        // Anthropic: { "error": { "message": "..." } } (similar to OpenAI)
                        if (errorObj.TryGetProperty("message", out var message2))
                            return message2.GetString() ?? $"HTTP error: {response.StatusCode}";

                        // Google: { "error": { "message": "..." } } (similar)
                        if (errorObj.TryGetProperty("message", out var message3))
                            return message3.GetString() ?? $"HTTP error: {response.StatusCode}";
                    }

                    // Fallback: try any top-level "message"
                    if (root.TryGetProperty("message", out var topMessage))
                        return topMessage.GetString() ?? $"HTTP error: {response.StatusCode}";

                    // If we can't find a message, return the raw body
                    return $"HTTP error: {response.StatusCode} - {errorJson}";
                }
                catch (JsonException)
                {
                    // Not JSON — use raw body
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
            var provider = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(providerType.ToString());

            var errorMessage = await ExtractErrorMessage(response, cancellationToken);

            var exception = new LLMConnectException(provider, errorMessage);

            logger?.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }
    }
}
