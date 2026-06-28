using LLMConnect.Settings;

namespace LLMConnect;

internal interface ILLMProviderOptionsValidator
{
    void Validate(LLMConnectClientOptions options);
}