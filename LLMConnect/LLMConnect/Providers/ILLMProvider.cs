using LLMConnect.Models;

namespace LLMConnect;

internal interface ILLMProvider
{
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken);
    IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken);
}
