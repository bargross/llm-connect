using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal static class OptionsValidator
{
    public static void Validate(LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions endpointOpts, ILogger? logger = null) 
        => OptionsValidatorFactory.Create(generalOpts.Provider).Validate(generalOpts, endpointOpts, logger);
}