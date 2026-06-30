using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http.Headers;
using Xunit;

namespace LLMConnect.Tests;

public class HttpClientConfiguratorTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;

    public HttpClientConfiguratorTests()
    {
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());
    }

    // ---------- ConfigureForProvider ----------

    [Theory]
    [InlineData(ProviderType.OpenAI, "https://api.openai.com/v1/chat/completions")]
    [InlineData(ProviderType.Anthropic, "https://api.anthropic.com/v1/messages")]
    [InlineData(ProviderType.Google, "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent")]
    [InlineData(ProviderType.Ollama, "http://localhost:11434/api/chat")]
    public void ConfigureForProvider_SetsBaseAddressCorrectly(ProviderType provider, string expectedBaseAddress)
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = provider,
            ApiKey = "test-key",
            Timeout = TimeSpan.FromSeconds(30),
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new HttpClient();

        // Act
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(options, client);

        // Assert
        configuredClient.BaseAddress.Should().NotBeNull();
        configuredClient.BaseAddress!.ToString().Should().Be(expectedBaseAddress);
    }

    [Fact]
    public void ConfigureForProvider_WhenCustomEndpointProvided_UsesCustomEndpoint()
    {
        // Arrange
        var customEndpoint = "https://custom-proxy.com/v1/chat";
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            Endpoint = customEndpoint,
            Timeout = TimeSpan.FromSeconds(30),
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new HttpClient();

        // Act
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(options, client);

        // Assert
        configuredClient.BaseAddress.Should().NotBeNull();
        configuredClient.BaseAddress!.ToString().Should().Be(customEndpoint);
    }

    [Fact]
    public void ConfigureForProvider_ForOllama_WithPort_UsesCustomPortInBaseAddress()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Ollama,
            OllamaPort = 11435,
            Timeout = TimeSpan.FromSeconds(30),
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new HttpClient();

        // Act
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(options, client);

        // Assert
        configuredClient.BaseAddress.Should().NotBeNull();
        configuredClient.BaseAddress!.ToString().Should().Be("http://localhost:11435/api/chat");
    }

    [Fact]
    public void ConfigureForProvider_ForOllama_WithNoPort_UsesDefaultPortInBaseAddress()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Ollama,
            OllamaPort = null,
            Timeout = TimeSpan.FromSeconds(30),
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new HttpClient();

        // Act
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(options, client);

        // Assert
        configuredClient.BaseAddress.Should().NotBeNull();
        configuredClient.BaseAddress!.ToString().Should().Be("http://localhost:11434/api/chat");
    }

    [Fact]
    public void ConfigureForProvider_ForOllama_WithCustomEndpointAndPort_IgnoresPort()
    {
        // Arrange
        var customEndpoint = "http://custom-ollama:11435/api/chat";
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Ollama,
            OllamaPort = 9999, // Should be ignored because Endpoint is set
            Endpoint = customEndpoint,
            Timeout = TimeSpan.FromSeconds(30),
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new HttpClient();

        // Act
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(options, client);

        // Assert
        configuredClient.BaseAddress.Should().NotBeNull();
        configuredClient.BaseAddress!.ToString().Should().Be(customEndpoint);
    }

    [Theory]
    [InlineData(ProviderType.OpenAI, "Authorization", "Bearer test-key")]
    [InlineData(ProviderType.Anthropic, "x-api-key", "test-key")]
    [InlineData(ProviderType.Google, "x-goog-api-key", "test-key")]
    public void ConfigureForProvider_AddsCorrectAuthenticationHeader(ProviderType provider, string headerName, string expectedValue)
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = provider,
            ApiKey = "test-key",
            Timeout = TimeSpan.FromSeconds(30),
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new HttpClient();

        // Act
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(options, client);

        // Assert
        configuredClient.DefaultRequestHeaders.Should().ContainKey(headerName);
        configuredClient.DefaultRequestHeaders.GetValues(headerName).Should().Contain(expectedValue);
    }

    [Fact]
    public void ConfigureForProvider_ForOllama_DoesNotAddAuthenticationHeader()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Ollama,
            ApiKey = null,
            Timeout = TimeSpan.FromSeconds(30),
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new HttpClient();

        // Act
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(options, client);

        // Assert
        configuredClient.DefaultRequestHeaders.Should().NotContainKey("Authorization");
        configuredClient.DefaultRequestHeaders.Should().NotContainKey("x-api-key");
        configuredClient.DefaultRequestHeaders.Should().NotContainKey("x-goog-api-key");
    }

    [Fact]
    public void ConfigureForProvider_SetsAcceptHeaderToApplicationJson()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            Timeout = TimeSpan.FromSeconds(30),
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new HttpClient();

        // Act
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(options, client);

        // Assert
        configuredClient.DefaultRequestHeaders.Accept.Should().Contain(a => a.MediaType == "application/json");
    }

    [Fact]
    public void ConfigureForProvider_SetsTimeout()
    {
        // Arrange
        var expectedTimeout = TimeSpan.FromSeconds(45);
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            Timeout = expectedTimeout,
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new HttpClient();

        // Act
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(options, client);

        // Assert
        configuredClient.Timeout.Should().Be(expectedTimeout);
    }

    [Fact]
    public void ConfigureForProvider_AddsUserAgentIfNotPresent()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            Timeout = TimeSpan.FromSeconds(30),
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new HttpClient();

        // Act
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(options, client);

        // Assert
        configuredClient.DefaultRequestHeaders.UserAgent.Should().Contain(ua =>
            ua.Product.Name == "LLMConnect" && ua.Product.Version == "1.0.0");
    }

    [Fact]
    public void ConfigureForProvider_DoesNotOverrideExistingUserAgent()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            Timeout = TimeSpan.FromSeconds(30),
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CustomApp", "2.0"));

        // Act
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(options, client);

        // Assert
        configuredClient.DefaultRequestHeaders.UserAgent.Should().Contain(ua =>
            ua.Product.Name == "CustomApp" && ua.Product.Version == "2.0");
        configuredClient.DefaultRequestHeaders.UserAgent.Should().NotContain(ua =>
            ua.Product.Name == "LLMConnect");
    }

    [Fact]
    public void ConfigureForProvider_ForAnthropic_AddsAnthropicVersionHeader()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.Anthropic,
            ApiKey = "test-key",
            Timeout = TimeSpan.FromSeconds(30),
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new HttpClient();

        // Act
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(options, client);

        // Assert
        configuredClient.DefaultRequestHeaders.Should().ContainKey("anthropic-version");
        configuredClient.DefaultRequestHeaders.GetValues("anthropic-version").Should().Contain("2023-06-01");
    }

    [Fact]
    public void ConfigureForProvider_ForUnsupportedProvider_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedProvider = (ProviderType)999;
        var options = new LLMConnectClientOptions
        {
            Provider = unsupportedProvider,
            ApiKey = "test-key",
            Timeout = TimeSpan.FromSeconds(30),
            LoggerFactory = _loggerFactoryMock.Object
        };
        var client = new HttpClient();

        // Act
        Action act = () => HttpClientConfigurator.ConfigureForProvider(options, client);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Provider '{unsupportedProvider}' is not supported.");
    }

    [Fact]
    public void ConfigureForProvider_WhenLoggerFactoryIsNull_DoesNotThrow()
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            Timeout = TimeSpan.FromSeconds(30),
            LoggerFactory = null
        };
        var client = new HttpClient();

        // Act
        Action act = () => HttpClientConfigurator.ConfigureForProvider(options, client);

        // Assert
        act.Should().NotThrow();
    }
}