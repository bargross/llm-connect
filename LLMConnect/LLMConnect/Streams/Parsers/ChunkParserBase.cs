using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal abstract class ChunkParserBase<TRoot>
{
    protected ILogger<TRoot>? _logger;

    public ChunkParserBase() { }

    public ChunkParserBase(LLMConnectClientOptions options)
    {
        _logger = options.LoggerFactory?.CreateLogger<TRoot>();
    }
}
