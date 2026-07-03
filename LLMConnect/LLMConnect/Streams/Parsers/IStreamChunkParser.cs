using LLMConnect.Models;

namespace LLMConnect;

public interface IStreamChunkParser
{
    ChatChunk? Parse(StreamEvent evt);
}