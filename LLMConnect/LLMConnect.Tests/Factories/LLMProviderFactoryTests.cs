using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Factories;

public class LLMProviderFactoryTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectGeneralOptions _validGeneralOptions;
    private readonly LLMConnectEndpointOptions _validEndpointOptions;

    public LLMProviderFactoryTests()
    {
        _loggerMock = new Mock<ILogger>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);

        _validGeneralOptions = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object
        };
        _validEndpointOptions = new LLMConnectEndpointOptions();
    }

    // ---------- Constructor with HttpClient ----------

    [Fact]
    public void Constructor_WithHttpClient_WhenGeneralOptionsNull_ThrowsArgumentNullException()
    {
        var act = () => new LLMProviderFactory(null, _validEndpointOptions, new HttpClient());
        act.Should().Throw<ArgumentNullException>().WithParameterName("generalOpts");
    }

    [Fact]
    public void Constructor_WithHttpClient_WhenEndpointOptionsNull_ThrowsArgumentNullException()
    {
        var act = () => new LLMProviderFactory(_validGeneralOptions, null, new HttpClient());
        act.Should().Throw<ArgumentNullException>().WithParameterName("endpointOpts");
    }

    [Fact]
    public void Constructor_WithHttpClient_WhenHttpClientNull_ThrowsArgumentNullException()
    {
        var act = () => new LLMProviderFactory(_validGeneralOptions, _validEndpointOptions, null as HttpClient);

        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithHttpClient_WithInvalidOptions_ThrowsValidationException()
    {
        var invalidOptions = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = null // missing API key
        };
        var act = () => new LLMProviderFactory(invalidOptions, _validEndpointOptions, new HttpClient());
        act.Should().Throw<ArgumentException>().WithMessage("*API key*");
    }

    // ---------- Constructor with IHttpClientFactory ----------

    [Fact]
    public void Constructor_WithHttpClientFactory_WhenGeneralOptionsNull_ThrowsArgumentNullException()
    {
        var factoryMock = new Mock<IHttpClientFactory>();
        var act = () => new LLMProviderFactory(null, _validEndpointOptions, factoryMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("generalOpts");
    }

    [Fact]
    public void Constructor_WithHttpClientFactory_WhenEndpointOptionsNull_ThrowsArgumentNullException()
    {
        var factoryMock = new Mock<IHttpClientFactory>();
        var act = () => new LLMProviderFactory(_validGeneralOptions, null, factoryMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("endpointOpts");
    }

    [Fact]
    public void Constructor_WithHttpClientFactory_WhenFactoryReturnsNull_ThrowsInvalidOperationException()
    {
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(x => x.CreateClient("LLMConnect")).Returns((HttpClient)null!);
        var act = () => new LLMProviderFactory(_validGeneralOptions, _validEndpointOptions, factoryMock.Object);
        act.Should().Throw<InvalidOperationException>().WithMessage("Failed to create HttpClient from factory.");
    }

    [Fact]
    public void Constructor_WithHttpClientFactory_ValidOptions_CreatesClient()
    {
        var expectedClient = new HttpClient();
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(x => x.CreateClient("LLMConnect")).Returns(expectedClient);

        var factory = new LLMProviderFactory(_validGeneralOptions, _validEndpointOptions, factoryMock.Object);
        factory.Should().NotBeNull();
    }

    // ---------- CreateProvider ----------

    [Theory]
    [InlineData(ProviderType.OpenAI, typeof(OpenAIProvider))]
    [InlineData(ProviderType.Anthropic, typeof(AnthropicProvider))]
    [InlineData(ProviderType.Google, typeof(GoogleProvider))]
    [InlineData(ProviderType.Ollama, typeof(OllamaProvider))]
    public void CreateProvider_ForSupportedProvider_ReturnsCorrectProviderType(ProviderType provider, Type expectedType)
    {
        var general = new LLMConnectGeneralOptions
        {
            Provider = provider,
            ApiKey = provider != ProviderType.Ollama ? "test-key" : null,
            LoggerFactory = _loggerFactoryMock.Object
        };
        var factory = new LLMProviderFactory(general, _validEndpointOptions, new HttpClient());
        var (client, providerInstance) = factory.CreateProvider();

        providerInstance.Should().BeOfType(expectedType);
        client.Should().NotBeNull();
    }

    [Fact]
    public void CreateProvider_ForUnsupportedProvider_ThrowsNotSupportedException()
    {
        var general = new LLMConnectGeneralOptions
        {
            Provider = (ProviderType)999,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object
        };

        var act = () => new LLMProviderFactory(general, _validEndpointOptions, new HttpClient()).CreateProvider();
        act.Should().Throw<NotSupportedException>().Which.Message.Should().Contain("999");
    }

    [Fact]
    public void CreateProvider_ConfiguresHttpClient()
    {
        var httpClient = new HttpClient();
        var factory = new LLMProviderFactory(_validGeneralOptions, _validEndpointOptions, httpClient);
        var (configuredClient, _) = factory.CreateProvider();

        // Verify that the client's BaseAddress is set (by HttpClientConfigurator)
        configuredClient.BaseAddress.Should().NotBeNull();
        configuredClient.BaseAddress!.ToString().Should().Be("https://api.openai.com/v1/");
    }

    [Fact]
    public void CreateProvider_ForAzureOpenAI_ReturnsAzureOpenAIProvider()
    {
        // Arrange
        var httpClient = new HttpClient();
        var generalOptions = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.AzureOpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object,
            DefaultModel = "gpt-4"
        };
        var endpointOptions = new LLMConnectEndpointOptions
        {
            AzureResourceName = "my-resource",
            AzureDeploymentName = "my-deployment",
            AzureApiVersion = "2024-02-15-preview"
        };

        // Act
        var (_, provider) = new LLMProviderFactory(generalOptions, endpointOptions, httpClient).CreateProvider();

        // Assert
        provider.Should().BeOfType<AzureOpenAIProvider>();
    }
}