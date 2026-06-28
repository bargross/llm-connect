using LLMConnect.Models;

namespace LLMConnect;

public interface ILLMConnectClient
{
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
}