using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect;

internal static class LLMConnectOptionsValidator
{
    public static void Validate(LLMConnectClientOptions options) => OptionsValidatorFactory.Create(options.Provider).Validate(options);
}