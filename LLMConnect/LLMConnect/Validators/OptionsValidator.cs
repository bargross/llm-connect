
using LLMConnect.Settings;
using LLMConnect.Validators.Options;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal static class OptionsValidator
{
    internal static EndpointOptionsValidator _endpointOptionsValidator = new();
    internal static GeneralOptionsValidator _generalOptionsValidator = new();

    public static void Validate(LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions endpointOpts, ILogger? logger = null)
    {
        _generalOptionsValidator.Validate(generalOpts, logger);

        _endpointOptionsValidator.Validate(endpointOpts, generalOpts.Provider.Value, logger);
    }
}