using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect.Validators.Options;

internal class GoogleOptionsValidator : LLMConnectOptionsValidationBase, IOptionsValidator
{
    protected override void ValidateProviderSpecific(LLMConnectClientOptions options, ILogger? logger = null) { }
}