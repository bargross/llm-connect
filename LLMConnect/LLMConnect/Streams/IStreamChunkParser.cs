using LLMConnect.Models;

namespace LLMConnect;

/// <summary>
/// Defines a parser for converting streamed events into chat chunks.
/// </summary>
public interface IStreamChunkParser
{
    /// <summary>
    /// Parses a streamed event into a chat chunk, if applicable.
    /// </summary>
    /// <param name="evt">The streamed event to parse.</param>
    /// <returns>The parsed chat chunk, or null if the event is not applicable.</returns>
    ChatChunk? Parse(StreamEvent evt);
}