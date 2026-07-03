using LLMConnect.Models;

namespace LLMConnect;

internal static class EmbeddingRequestMappingExtensions
{
    // ---------- OpenAI ----------
    internal static OpenAIEmbeddingRequest ToOpenAIRequest(this EmbeddingRequest request, string? defaultModel = null)
    {
        if (request is null) return null;

        var model = request.Model ?? defaultModel ?? "text-embedding-3-small";

        var openAiRequest = new OpenAIEmbeddingRequest
        {
            Model = model,
            Input = request.Text,
            EncodingFormat = request.EncodingFormat ?? "float",
            Dimensions = request.Dimensions,
            User = request.User
        };

        // Merge extra parameters (override if present)
        if (request.ExtraParameters != null && request.ExtraParameters.Count > 0)
        {
            // 'encoding_format' and 'dimensions' are already handled, but we allow override via ExtraParameters
            if (request.ExtraParameters.TryGetValue("encoding_format", out var format) && format is string encFormat)
                openAiRequest.EncodingFormat = encFormat;

            if (request.ExtraParameters.TryGetValue("dimensions", out var dims) && dims is int dimensions)
                openAiRequest.Dimensions = dimensions;

            if (request.ExtraParameters.TryGetValue("user", out var user) && user is string userStr)
                openAiRequest.User = userStr;
        }

        return openAiRequest;
    }

    // ---------- Google ----------
    internal static GoogleEmbeddingRequest ToGoogleRequest(this EmbeddingRequest request, string? defaultModel = null)
    {
        if (request is null) return null;

        var googleRequest = new GoogleEmbeddingRequest
        {
            Content = new GoogleEmbeddingContent
            {
                Parts = new List<GoogleEmbeddingPart>
                {
                    new GoogleEmbeddingPart { Text = request.Text }
                },
                Role = request.Role
            },
            TaskType = request.TaskType,
            Title = request.Title
        };

        // Merge extra parameters
        if (request.ExtraParameters != null && request.ExtraParameters.Count > 0)
        {
            if (request.ExtraParameters.TryGetValue("taskType", out var taskType) && taskType is string tt)
                googleRequest.TaskType = tt;

            if (request.ExtraParameters.TryGetValue("title", out var title) && title is string t)
                googleRequest.Title = t;

            if (request.ExtraParameters.TryGetValue("role", out var role) && role is string r)
                googleRequest.Content.Role = r;
        }

        return googleRequest;
    }

    // ---------- Ollama ----------
    internal static OllamaEmbeddingRequest ToOllamaRequest(this EmbeddingRequest request, string? defaultModel = null)
    {
        if (request is null) return null;

        var model = request.Model ?? defaultModel;

        var ollamaRequest = new OllamaEmbeddingRequest
        {
            Model = model,
            Prompt = request?.Text
        };

        // Pass all extra parameters as options
        if (request.ExtraParameters != null && request.ExtraParameters.Count > 0)
        {
            ollamaRequest.Options = new Dictionary<string, object>();
            foreach (var kvp in request.ExtraParameters)
            {
                ollamaRequest.Options[kvp.Key] = kvp.Value;
            }
        }

        return ollamaRequest;
    }
}