using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect.Tests.Registries
{
    public class EndpointRegistryTests
    {
        private readonly LLMConnectEndpointOptions _endpointOptions;

        public EndpointRegistryTests()
        {
            _endpointOptions = new LLMConnectEndpointOptions();
        }

        // ---------- GetDefaultEndpoint ----------

        [Theory]
        [InlineData(ProviderType.OpenAI, "https://api.openai.com/v1/")]
        [InlineData(ProviderType.Anthropic, "https://api.anthropic.com/v1/")]
        [InlineData(ProviderType.Google, "https://generativelanguage.googleapis.com/v1beta/")]
        public void GetDefaultEndpoint_ForStandardProviders_ReturnsBaseUrlWithTrailingSlash(ProviderType provider, string expected)
        {
            var result = EndpointRegistry.GetDefaultEndpoint(provider, _endpointOptions);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetDefaultEndpoint_ForOllama_ReturnsLocalhostWithDefaultPort()
        {
            var result = EndpointRegistry.GetDefaultEndpoint(ProviderType.Ollama, _endpointOptions);
            Assert.Equal("http://localhost:11434/", result);
        }

        [Fact]
        public void GetDefaultEndpoint_ForOllama_WithCustomPort_UsesProvidedPort()
        {
            _endpointOptions.OllamaPort = 12345;
            var result = EndpointRegistry.GetDefaultEndpoint(ProviderType.Ollama, _endpointOptions);
            Assert.Equal("http://localhost:12345/", result);
        }

        [Fact]
        public void GetDefaultEndpoint_ForAzureOpenAI_ReplacesResourceAndDeployment()
        {
            _endpointOptions.AzureResourceName = "my-resource";
            _endpointOptions.AzureDeploymentName = "gpt-4";
            var result = EndpointRegistry.GetDefaultEndpoint(ProviderType.AzureOpenAI, _endpointOptions);
            Assert.Equal("https://my-resource.openai.azure.com/openai/deployments/gpt-4/", result);
        }

        [Fact]
        public void GetDefaultEndpoint_ForAzureOpenAI_WithMissingPlaceholders_LeavesEmpty()
        {
            // If resource/deployment are null, they become empty strings
            var result = EndpointRegistry.GetDefaultEndpoint(ProviderType.AzureOpenAI, _endpointOptions);
            Assert.Equal("https://.openai.azure.com/openai/deployments//", result);
            // (This is an edge case; validation upstream ensures they're not null)
        }

        [Fact]
        public void GetDefaultEndpoint_ForUnknownProvider_ThrowsNotSupportedException()
        {
            var unknown = (ProviderType)999;
            Assert.Throws<NotSupportedException>(() =>
                EndpointRegistry.GetDefaultEndpoint(unknown, _endpointOptions));
        }

        // ---------- GetEndpointParams ----------

        [Theory]
        [InlineData(ProviderType.OpenAI, (int)QueryType.Chat, false, "chat/completions")]
        [InlineData(ProviderType.OpenAI, (int)QueryType.Chat, true, "chat/completions")]
        [InlineData(ProviderType.OpenAI, (int)QueryType.Embeddings, false, "embeddings")]
        [InlineData(ProviderType.Anthropic, (int)QueryType.Chat, false, "messages")]
        [InlineData(ProviderType.Anthropic, (int)QueryType.Chat, true, "messages")]
        [InlineData(ProviderType.Ollama, (int)QueryType.Chat, false, "api/chat")]
        [InlineData(ProviderType.Ollama, (int)QueryType.Chat, true, "api/chat")]
        [InlineData(ProviderType.Ollama, (int)QueryType.Embeddings, false, "api/embed")]
        [InlineData(ProviderType.AzureOpenAI, (int)QueryType.Chat, false, "chat/completions")]
        [InlineData(ProviderType.AzureOpenAI, (int)QueryType.Chat, true, "chat/completions")]
        [InlineData(ProviderType.AzureOpenAI, (int)QueryType.Embeddings, false, "embeddings")]
        public void GetEndpointParams_ReturnsCorrectPath(ProviderType provider, int query, bool streaming, string expected)
        {
            var result = EndpointRegistry.GetEndpointParams(provider, (QueryType)query, streaming);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ProviderType.Google, (int)QueryType.Chat, false, "gemini-pro", "models/gemini-pro:generateContent")]
        [InlineData(ProviderType.Google, (int)QueryType.Chat, true, "gemini-pro", "models/gemini-pro:streamGenerateContent")]
        [InlineData(ProviderType.Google, (int)QueryType.Embeddings, false, "gemini-embed", "models/gemini-embed:embedContent")]
        public void GetEndpointParams_ForGoogle_ReplacesModelPlaceholder(ProviderType provider, int query, bool streaming, string model, string expected)
        {
            var result = EndpointRegistry.GetEndpointParams(provider, (QueryType)query, streaming, model);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetEndpointParams_ForGoogle_WithNullModel_LeavesPlaceholder()
        {
            var result = EndpointRegistry.GetEndpointParams(ProviderType.Google, QueryType.Chat, false, null);
            Assert.Equal("models/{model}:generateContent", result);
        }

        [Fact]
        public void GetEndpointParams_ForUnsupportedCombination_ThrowsNotSupportedException()
        {
            // For example, OpenAI doesn't support Embeddings with streaming
            var ex = Assert.Throws<NotSupportedException>(() =>
                EndpointRegistry.GetEndpointParams(ProviderType.OpenAI, QueryType.Embeddings, true));
            Assert.Contains("not supported", ex.Message);
        }
    }
}