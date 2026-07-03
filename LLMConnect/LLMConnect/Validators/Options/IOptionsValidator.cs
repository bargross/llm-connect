using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect.Validators.Options;

internal interface IOptionsValidator
{
    void Validate(LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions endpointOpts, ILogger? logger = null);
}