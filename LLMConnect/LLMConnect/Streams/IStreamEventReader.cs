namespace LLMConnect;

/// <summary>
/// Defines a reader for streaming events from a stream.
/// </summary>
public interface IStreamEventReader
{
    /// <summary>
    /// Reads streamed events from the provided stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to read events from.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of streamed events.</returns>
    IAsyncEnumerable<StreamEvent> ReadEventsAsync(
        Stream stream,
        CancellationToken cancellationToken = default);
}