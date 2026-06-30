namespace LLMConnect.Streams.StreamReaders;

internal interface IStreamEventReader
{
    IAsyncEnumerable<StreamEvent> ReadEventsAsync(
        Stream stream,
        CancellationToken cancellationToken = default);
}