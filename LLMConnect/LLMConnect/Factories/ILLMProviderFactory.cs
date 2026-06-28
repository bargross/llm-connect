namespace LLMConnect;

internal interface ILLMProviderFactory
{
    (HttpClient, ILLMProvider) CreateProvider();
}