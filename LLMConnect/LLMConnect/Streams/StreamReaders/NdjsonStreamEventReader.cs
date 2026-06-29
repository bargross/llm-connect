using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace LLMConnect.Streams.StreamReaders;

internal class NdjsonStreamEventReader : IStreamEventReader
{
    private readonly ILogger<NdjsonStreamEventReader>? _logger;

    public NdjsonStreamEventReader() { }
    public NdjsonStreamEventReader(LLMConnectClientOptions options)
    {
        _logger = options.LoggerFactory?.CreateLogger<NdjsonStreamEventReader>();
    }

    public async IAsyncEnumerable<StreamEvent> ReadEventsAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream);

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

            // OpenAI uses "data: " prefix; Ollama uses raw JSON lines
            if (line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase))
            {
                var data = line.Substring(6).Trim();
                if (data == "[DONE]")
                {
                    yield return new StreamEvent(null, data);

                    yield break;
                }

                yield return new StreamEvent(null, data);
            }
            else
            {
                // Raw JSON line (Ollama style)
                yield return new StreamEvent(null, line);
            }
        }
    }
}