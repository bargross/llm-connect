using LLMConnect.Models;
using LLMConnect.Settings;
using Moq;
using Moq.Protected;
using System.Net;

namespace LLMConnect.Tests.Integration
{
    public class EndpointIntegrationTests
    {
        // All supported providers (matching the library's ProviderType enum)
        public static IEnumerable<object[]> AllProviders => new List<object[]>
        {
            new object[] { ProviderType.OpenAI, "https://my-proxy.com/openai" },
            new object[] { ProviderType.Anthropic, "https://my-proxy.com/anthropic" },
            new object[] { ProviderType.Google, "https://my-proxy.com/google" },
            new object[] { ProviderType.Ollama, "https://my-proxy.com/ollama" },
        };

        [Theory]
        [MemberData(nameof(AllProviders))]
        public async Task ChatAsync_UsesCustomEndpoint_WhenProvided(ProviderType provider, string customEndpoint)
        {
            // Arrange
            Uri? capturedRequestUri = null;
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
                {
                    capturedRequestUri = req.RequestUri;
                })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        @"{""id"":""test"",""choices"":[{""message"":{""content"":""ok""}}]}"
                    )
                });

            var httpClient = new HttpClient(handlerMock.Object);

            var options = new LLMConnectClientOptions
            {
                Provider = provider,
                ApiKey = GetDummyApiKey(provider),
                DefaultModel = GetDummyModel(provider),
                Endpoint = customEndpoint
            };

            var client = new LLMConnectClient(options, httpClient);

            var request = new ChatRequest
            {
                Messages = new List<Message> { new UserMessage("Hello, world!") }
            };

            // Act
            await client.ChatAsync(request);

            // Assert
            Assert.NotNull(capturedRequestUri);
            Assert.StartsWith(customEndpoint, capturedRequestUri.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ChatAsync_UsesDefaultEndpoint_WhenEndpointIsNull()
        {
            foreach (var provider in new[] { ProviderType.OpenAI, ProviderType.Anthropic, ProviderType.Google, ProviderType.Ollama })
            {
                // Arrange
                Uri? capturedRequestUri = null;
                var handlerMock = new Mock<HttpMessageHandler>();

                handlerMock
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>()
                    )
                    .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
                    {
                        capturedRequestUri = req.RequestUri;
                    })
                    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            @"{""id"":""test"",""choices"":[{""message"":{""content"":""ok""}}]}"
                        )
                    });

                var httpClient = new HttpClient(handlerMock.Object);

                var options = new LLMConnectClientOptions
                {
                    Provider = provider,
                    ApiKey = GetDummyApiKey(provider),
                    DefaultModel = GetDummyModel(provider),
                    Endpoint = null // Explicitly null
                };

                var client = new LLMConnectClient(options, httpClient);

                var request = new ChatRequest
                {
                    Messages = new List<Message> { new UserMessage("Hi") }
                };

                // Act
                await client.ChatAsync(request);

                // Assert
                Assert.NotNull(capturedRequestUri);
                var expectedDefault = GetDefaultEndpointForProvider(provider);
                Assert.StartsWith(expectedDefault, capturedRequestUri.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        [Theory]
        [InlineData(ProviderType.OpenAI, "https://my-proxy.com/v1/")]
        [InlineData(ProviderType.Anthropic, "https://my-proxy.com/v1/")]
        [InlineData(ProviderType.Google, "https://my-proxy.com/v1/")]
        [InlineData(ProviderType.Ollama, "http://localhost:11434/")]
        public async Task ChatAsync_NormalizesTrailingSlash_FromCustomEndpoint(ProviderType provider, string customEndpointWithSlash)
        {
            // Arrange
            Uri? capturedRequestUri = null;
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
                {
                    capturedRequestUri = req.RequestUri;
                })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        @"{""id"":""test"",""choices"":[{""message"":{""content"":""ok""}}]}"
                    )
                });

            var httpClient = new HttpClient(handlerMock.Object);

            var options = new LLMConnectClientOptions
            {
                Provider = provider,
                ApiKey = GetDummyApiKey(provider),
                DefaultModel = GetDummyModel(provider),
                Endpoint = customEndpointWithSlash
            };

            var client = new LLMConnectClient(options, httpClient);

            var request = new ChatRequest
            {
                Messages = new List<Message> { new UserMessage("Test") }
            };

            // Act
            await client.ChatAsync(request);

            // Assert
            Assert.NotNull(capturedRequestUri);
            // The library should not double-slash; it should result in a clean URL.
            // For example, https://my-proxy.com/v1/path, not https://my-proxy.com/v1//path
            var expectedBase = customEndpointWithSlash.TrimEnd('/');
            Assert.StartsWith(expectedBase, capturedRequestUri.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(ProviderType.OpenAI)]
        [InlineData(ProviderType.Anthropic)]
        [InlineData(ProviderType.Google)]
        public void Endpoint_MustUseHttps_ForCloudProviders(ProviderType provider)
        {
            // Arrange
            var options = new LLMConnectClientOptions
            {
                Provider = provider,
                ApiKey = GetDummyApiKey(provider),
                DefaultModel = GetDummyModel(provider),
                Endpoint = "http://insecure-proxy.com/v1" // HTTP, not HTTPS
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new LLMConnectClient(options));
            Assert.Contains("HTTPS", exception.Message);
        }

        [Fact]
        public void Endpoint_AllowsHttp_ForOllama()
        {
            // Arrange
            var options = new LLMConnectClientOptions
            {
                Provider = ProviderType.Ollama,
                Endpoint = "http://localhost:11434/api/chat", // HTTP is allowed for Ollama
                DefaultModel = "llama3.2"
            };

            // Act & Assert (no exception)
            var exception = Record.Exception(() => new LLMConnectClient(options));
            Assert.Null(exception);
        }

        [Fact]
        public void Endpoint_MustBeValidAbsoluteUrl()
        {
            // Arrange
            var options = new LLMConnectClientOptions
            {
                Provider = ProviderType.OpenAI,
                ApiKey = "dummy",
                DefaultModel = "gpt-4",
                Endpoint = "not-a-valid-url"
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new LLMConnectClient(options));
            Assert.Contains("Invalid endpoint URL", exception.Message);
        }

        // --- Helper Methods ---

        private static string GetDummyApiKey(ProviderType provider) =>
            provider == ProviderType.Ollama ? "" : "dummy-api-key";

        private static string GetDummyModel(ProviderType provider) =>
            provider switch
            {
                ProviderType.OpenAI => "gpt-4",
                ProviderType.Anthropic => "claude-3-5-sonnet-20241022",
                ProviderType.Google => "gemini-2.0-flash",
                ProviderType.Ollama => "llama3.2",
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
            };

        private static string GetDefaultEndpointForProvider(ProviderType provider) =>
            provider switch
            {
                ProviderType.OpenAI => "https://api.openai.com/v1",
                ProviderType.Anthropic => "https://api.anthropic.com/v1",
                ProviderType.Google => "https://generativelanguage.googleapis.com/v1beta",
                ProviderType.Ollama => "http://localhost:11434/api/chat",
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
            };
    }
}