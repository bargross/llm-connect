using LLMConnect.Settings;

namespace LLMConnect;

internal class AnthropicOptionsValidator : LLMConnectOptionsValidationBase, ILLMProviderOptionsValidator
{
    protected override void ValidateProviderSpecific(LLMConnectClientOptions options) { }
}