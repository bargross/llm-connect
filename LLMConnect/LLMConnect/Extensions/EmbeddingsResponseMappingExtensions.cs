using LLMConnect.Models;

namespace LLMConnect;

internal static class EmbeddingResponseMappingExtensions
{
    internal static EmbeddingResponse ToEmbeddingResponse(this OpenAIEmbeddingResponse response)
    {
        var embedding = response.Data?.FirstOrDefault()?.Embedding ?? Array.Empty<float>();
        return new EmbeddingResponse
        {
            Embedding = embedding,
            Model = response.Model,
            Usage = response.Usage != null
                ? new EmbeddingUsage
                {
                    InputTokens = response.Usage.PromptTokens,
                    OutputTokens = 0,
                    TotalTokens = response.Usage.TotalTokens
                }
                : null,
            CreatedAt = DateTime.UtcNow
        };
    }

    internal static EmbeddingResponse ToEmbeddingResponse(this GoogleEmbeddingResponse response)
    {
        var embedding = response.Embedding?.Values ?? Array.Empty<float>();
        return new EmbeddingResponse
        {
            Embedding = embedding,
            Model = "google",
            Usage = null,
            CreatedAt = DateTime.UtcNow
        };
    }

    internal static EmbeddingResponse ToEmbeddingResponse(this OllamaEmbeddingResponse response)
    {
        var embedding = response.Embedding ?? Array.Empty<float>();
        return new EmbeddingResponse
        {
            Embedding = embedding,
            Model = "ollama",
            Usage = null,
            CreatedAt = DateTime.UtcNow
        };
    }
}