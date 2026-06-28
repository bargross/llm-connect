using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect;

internal class GoogleOptionsValidator : LLMConnectOptionsValidationBase, ILLMProviderOptionsValidator
{
    protected override void ValidateProviderSpecific(LLMConnectClientOptions options) { }
}