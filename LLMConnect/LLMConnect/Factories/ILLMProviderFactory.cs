using LLMConnect.Settings;

namespace LLMConnect;

internal interface ILLMProviderFactory
{
    ILLMProvider CreateProvider();
}