namespace LLMConnect;

public interface IStreamEventReader
{
    IAsyncEnumerable<StreamEvent> ReadEventsAsync(
        Stream stream,
        CancellationToken cancellationToken = default);
}