using LLMConnect.Models;

namespace LLMConnect;

internal interface ILLMProvider
{
    Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
    Task<EmbeddingResponse?> GetEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken = default);
}
