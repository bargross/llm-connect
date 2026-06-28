using LLMConnect.Settings;

namespace LLMConnect;

internal class OpenAIOptionsValidator : LLMConnectOptionsValidationBase, ILLMProviderOptionsValidator
{
    protected override void ValidateProviderSpecific(LLMConnectClientOptions options) { }
}