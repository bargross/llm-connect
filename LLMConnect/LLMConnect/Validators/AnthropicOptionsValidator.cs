using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal class AnthropicOptionsValidator : LLMConnectOptionsValidationBase, ILLMProviderOptionsValidator
{
    protected override void ValidateProviderSpecific(LLMConnectClientOptions options, ILogger? logger = null) { }
}