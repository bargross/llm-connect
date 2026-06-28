using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal interface ILLMProviderOptionsValidator
{
    void Validate(LLMConnectClientOptions options, ILogger? logger = null);
}