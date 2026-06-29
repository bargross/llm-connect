using LLMConnect.Models;

namespace LLMConnect;

internal interface IStreamChunkParser
{
    ChatChunk? Parse(StreamEvent evt);
}