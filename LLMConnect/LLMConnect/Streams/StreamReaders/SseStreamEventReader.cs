using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace LLMConnect.Streams.StreamReaders;

internal class SseStreamEventReader : IStreamEventReader
{
    private readonly ILogger<NdjsonStreamEventReader>? _logger;

    public SseStreamEventReader() { }
    public SseStreamEventReader(LLMConnectClientOptions options)
    {
        _logger = options.LoggerFactory?.CreateLogger<NdjsonStreamEventReader>();
    }

    public async IAsyncEnumerable<StreamEvent> ReadEventsAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream);

        string? currentEvent = null;
        string? line;
        var streaming = true;
        while (streaming)
        {
            try
            {
                line = await reader.ReadLineAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger?.LogError("OpenAI stream has ended.");

                break; // let the caller handle this on its own
            }

            streaming = line != null;

            if (string.IsNullOrEmpty(line))
                continue;

            if (line.StartsWith("event: ", StringComparison.OrdinalIgnoreCase))
            {
                currentEvent = line.Substring(7).Trim();
                continue;
            }

            if (line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase))
            {
                var data = line.Substring(6).Trim();
                if (data == "[DONE]")
                {
                    yield return new StreamEvent(currentEvent, data);
                    yield break;
                }

                yield return new StreamEvent(currentEvent, data);
                currentEvent = null; // Reset after yielding
            }
        }
    }
}