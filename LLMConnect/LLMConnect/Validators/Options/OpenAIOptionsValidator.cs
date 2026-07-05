using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect.Validators.Options;

internal class OpenAIOptionsValidator : OptionsValidationBase, IOptionsValidator
{
    protected override void ValidateProviderSpecificGeneralOptions(LLMConnectGeneralOptions generalOptions, ILogger? logger = null) { }
    protected override void ValidateProviderSpecificEndpointOptions(LLMConnectEndpointOptions endpointOptions, ILogger? logger = null) { }
}