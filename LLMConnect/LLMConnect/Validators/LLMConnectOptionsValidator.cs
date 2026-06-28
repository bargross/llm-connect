using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal static class LLMConnectOptionsValidator
{
    public static void Validate(LLMConnectClientOptions options, ILogger? logger = null) => OptionsValidatorFactory.Create(options.Provider).Validate(options, logger);
}