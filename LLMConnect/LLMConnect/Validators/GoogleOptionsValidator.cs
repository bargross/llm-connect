using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal class GoogleOptionsValidator : LLMConnectOptionsValidationBase, ILLMProviderOptionsValidator
{
    protected override void ValidateProviderSpecific(LLMConnectClientOptions options, ILogger? logger = null) { }
}