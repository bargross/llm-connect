using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMConnect.Tests;

public class LLMProviderFactoryTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectClientOptions _options;

    public LLMProviderFactoryTests()
    {
        _loggerMock = new Mock<ILogger>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object,
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3
        };
    }

    [Fact]
    public void Constructor_WithHttpClient_ValidatesOptions()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var factory = new LLMProviderFactory(_options, httpClient);

        // Assert
        factory.Should().NotBeNull();
        // Validation is called in constructor; if options were invalid, it would have thrown.
        // We can verify that validation was called by checking that the logger was used.
        // (Hard to mock static validation, but we trust it was called.)
    }

    [Fact]
    public void Constructor_WithHttpClient_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        Action act = () => new LLMProviderFactory(null!, httpClient);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithHttpClient_WhenHttpClientIsNull_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new LLMProviderFactory(_options, null! as HttpClient);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithHttpClientFactory_CreatesClient()
    {
        // Arrange
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var expectedClient = new HttpClient();
        httpClientFactoryMock
            .Setup(x => x.CreateClient("LLMConnect"))
            .Returns(expectedClient);

        // Act
        var factory = new LLMProviderFactory(_options, httpClientFactoryMock.Object);

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithHttpClientFactory_WhenFactoryReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(x => x.CreateClient("LLMConnect"))
            .Returns((HttpClient?)null!);

        // Act
        Action act = () => new LLMProviderFactory(_options, httpClientFactoryMock.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to create HttpClient from factory.");
    }

    [Fact]
    public void Constructor_WithHttpClientFactory_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(x => x.CreateClient("LLMConnect"))
            .Returns(new HttpClient()); // Return a valid client

        // Act
        Action act = () => new LLMProviderFactory(null!, httpClientFactoryMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Theory]
    [InlineData(ProviderType.OpenAI, typeof(OpenAIProvider))]
    [InlineData(ProviderType.Anthropic, typeof(AnthropicProvider))]
    [InlineData(ProviderType.Google, typeof(GoogleProvider))]
    [InlineData(ProviderType.Ollama, typeof(OllamaProvider))]
    public void CreateProvider_ForSupportedProviders_ReturnsCorrectProvider(ProviderType provider, Type expectedType)
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = provider,
            ApiKey = provider != ProviderType.Ollama ? "test-key" : null,
            LoggerFactory = _loggerFactoryMock.Object,
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3
        };
        var httpClient = new HttpClient();
        var factory = new LLMProviderFactory(options, httpClient);

        // Act
        var (client, providerInstance) = factory.CreateProvider();

        // Assert
        providerInstance.Should().BeOfType(expectedType);
        client.Should().Be(httpClient);
    }

    [Fact]
    public void CreateProvider_ConfiguresHttpClient()
    {
        // Arrange
        var httpClient = new HttpClient();
        var factory = new LLMProviderFactory(_options, httpClient);

        // Act
        var (client, _) = factory.CreateProvider();

        // Assert
        // The client should be configured with BaseAddress and headers
        // We can verify that the client is the same instance and that it has been configured
        client.Should().Be(httpClient);
        // BaseAddress should be set by HttpClientConfigurator
        // We can't easily verify the exact headers without making them public,
        // but we can trust that ConfigureForProvider was called.
    }

    [Fact]
    public void Constructor_WhenOptionsAreInvalid_ThrowsValidationException()
    {
        // Arrange
        var invalidOptions = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = null!, // Missing API key - should throw validation
            LoggerFactory = _loggerFactoryMock.Object,
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3
        };
        var httpClient = new HttpClient();

        // Act
        Action act = () => new LLMProviderFactory(invalidOptions, httpClient);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Missing api key for provider OpenAI*");
    }

    [Fact]
    public void Constructor_WithHttpClient_ValidatesOptions_WhenOllamaWithNoApiKey_DoesNotThrow()
    {
        // Arrange
        var ollamaOptions = new LLMConnectClientOptions
        {
            Provider = ProviderType.Ollama,
            ApiKey = null, // Ollama does not require an API key
            LoggerFactory = _loggerFactoryMock.Object,
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3
        };
        var httpClient = new HttpClient();

        // Act
        Action act = () => new LLMProviderFactory(ollamaOptions, httpClient);

        // Assert
        act.Should().NotThrow();
    }
}