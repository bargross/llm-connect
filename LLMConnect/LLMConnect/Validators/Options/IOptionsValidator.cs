using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect.Validators.Options;

internal interface IOptionsValidator
{
    void Validate(LLMConnectClientOptions options, ILogger? logger = null);
}