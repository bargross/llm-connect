using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http.Headers;

namespace LLMConnect.Tests.Configurators;

public class HttpClientConfiguratorTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectGeneralOptions _defaultGeneralOptions;
    private readonly LLMConnectEndpointOptions _defaultEndpointOptions;

    public HttpClientConfiguratorTests()
    {
        _loggerMock = new Mock<ILogger>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _defaultGeneralOptions = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object,
            Timeout = TimeSpan.FromSeconds(30)
        };

        _defaultEndpointOptions = new LLMConnectEndpointOptions();
    }

    // ---------- BaseAddress Resolution ----------

    [Theory]
    [InlineData(ProviderType.OpenAI, "https://api.openai.com/v1/")]
    [InlineData(ProviderType.Anthropic, "https://api.anthropic.com/v1/")]
    [InlineData(ProviderType.Google, "https://generativelanguage.googleapis.com/v1beta/")]
    [InlineData(ProviderType.Ollama, "http://localhost:11434/")]
    public void ConfigureForProvider_WithDefaultEndpoint_SetsCorrectBaseAddress(ProviderType provider, string expectedBaseAddress)
    {
        var general = new LLMConnectGeneralOptions
        {
            Provider = provider,
            ApiKey = provider != ProviderType.Ollama ? "test-key" : null,
            LoggerFactory = _loggerFactoryMock.Object,
            Timeout = TimeSpan.FromSeconds(30)
        };

        var endpoint = new LLMConnectEndpointOptions();
        var client = new HttpClient();

        var configuredClient = HttpClientConfigurator.ConfigureForProvider(general, endpoint, client);

        configuredClient.BaseAddress.Should().NotBeNull();
        configuredClient.BaseAddress!.ToString().Should().Be(expectedBaseAddress);
    }

    [Fact]
    public void ConfigureForProvider_WithCustomEndpoint_UsesCustomEndpoint()
    {
        var customEndpoint = "https://my-custom-proxy.com/v1/";
        var endpointOptions = new LLMConnectEndpointOptions { Endpoint = customEndpoint };
        var client = new HttpClient();

        var configuredClient = HttpClientConfigurator.ConfigureForProvider(_defaultGeneralOptions, endpointOptions, client);

        configuredClient.BaseAddress.Should().NotBeNull();
        configuredClient.BaseAddress!.ToString().Should().Be(customEndpoint);
    }

    [Fact]
    public void ConfigureForProvider_ForOllama_WithOllamaPort_UsesPortInBaseAddress()
    {
        var general = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Ollama,
            LoggerFactory = _loggerFactoryMock.Object,
            Timeout = TimeSpan.FromSeconds(30)
        };
        var endpoint = new LLMConnectEndpointOptions { OllamaPort = 11435 };
        var client = new HttpClient();

        var configuredClient = HttpClientConfigurator.ConfigureForProvider(general, endpoint, client);

        configuredClient.BaseAddress!.ToString().Should().Be("http://localhost:11435/");
    }

    [Fact]
    public void ConfigureForProvider_ForOllama_CustomEndpointOverridesOllamaPort()
    {
        var customEndpoint = "http://custom-ollama:11436/api/";
        var endpoint = new LLMConnectEndpointOptions
        {
            Endpoint = customEndpoint,
            OllamaPort = 11435 // Should be ignored
        };
        var general = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Ollama,
            LoggerFactory = _loggerFactoryMock.Object,
            Timeout = TimeSpan.FromSeconds(30)
        };
        var client = new HttpClient();

        var configuredClient = HttpClientConfigurator.ConfigureForProvider(general, endpoint, client);

        configuredClient.BaseAddress!.ToString().Should().Be(customEndpoint);
    }

    // ---------- Headers ----------

    [Theory]
    [InlineData(ProviderType.OpenAI, "Authorization", "Bearer test-key")]
    [InlineData(ProviderType.Anthropic, "x-api-key", "test-key")]
    [InlineData(ProviderType.Google, "x-goog-api-key", "test-key")]
    public void ConfigureForProvider_AddsCorrectAuthenticationHeader(ProviderType provider, string headerName, string expectedValue)
    {
        var general = new LLMConnectGeneralOptions
        {
            Provider = provider,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object,
            Timeout = TimeSpan.FromSeconds(30)
        };
        var endpoint = new LLMConnectEndpointOptions();
        var client = new HttpClient();

        var configuredClient = HttpClientConfigurator.ConfigureForProvider(general, endpoint, client);

        configuredClient.DefaultRequestHeaders.Should().ContainKey(headerName);
        configuredClient.DefaultRequestHeaders.GetValues(headerName).Should().Contain(expectedValue);
    }

    [Fact]
    public void ConfigureForProvider_ForAnthropic_AddsAnthropicVersionHeader()
    {
        var general = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Anthropic,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object,
            Timeout = TimeSpan.FromSeconds(30)
        };
        var endpoint = new LLMConnectEndpointOptions();
        var client = new HttpClient();

        var configuredClient = HttpClientConfigurator.ConfigureForProvider(general, endpoint, client);

        configuredClient.DefaultRequestHeaders.Should().ContainKey("anthropic-version");
        configuredClient.DefaultRequestHeaders.GetValues("anthropic-version").Should().Contain("2023-06-01");
    }

    [Fact]
    public void ConfigureForProvider_ForOllama_DoesNotAddAuthenticationHeader()
    {
        var general = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.Ollama,
            LoggerFactory = _loggerFactoryMock.Object,
            Timeout = TimeSpan.FromSeconds(30)
        };
        var endpoint = new LLMConnectEndpointOptions();
        var client = new HttpClient();

        var configuredClient = HttpClientConfigurator.ConfigureForProvider(general, endpoint, client);

        configuredClient.DefaultRequestHeaders.Should().NotContainKey("Authorization");
        configuredClient.DefaultRequestHeaders.Should().NotContainKey("x-api-key");
        configuredClient.DefaultRequestHeaders.Should().NotContainKey("x-goog-api-key");
    }

    [Fact]
    public void ConfigureForProvider_SetsAcceptHeaderToApplicationJson()
    {
        var client = new HttpClient();
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(_defaultGeneralOptions, _defaultEndpointOptions, client);

        configuredClient.DefaultRequestHeaders.Accept.Should().Contain(a => a.MediaType == "application/json");
    }

    // ---------- User-Agent ----------

    [Fact]
    public void ConfigureForProvider_AddsDefaultUserAgentIfNotPresent()
    {
        var client = new HttpClient();
        var configuredClient = HttpClientConfigurator.ConfigureForProvider(_defaultGeneralOptions, _defaultEndpointOptions, client);

        configuredClient.DefaultRequestHeaders.UserAgent.Should().Contain(ua =>
            ua.Product.Name == "LLMConnect" && ua.Product.Version == "1.0.0");
    }

    [Fact]
    public void ConfigureForProvider_PreservesExistingUserAgent()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CustomApp", "2.0"));

        var configuredClient = HttpClientConfigurator.ConfigureForProvider(_defaultGeneralOptions, _defaultEndpointOptions, client);

        configuredClient.DefaultRequestHeaders.UserAgent.Should().Contain(ua =>
            ua.Product.Name == "CustomApp" && ua.Product.Version == "2.0");
        configuredClient.DefaultRequestHeaders.UserAgent.Should().NotContain(ua =>
            ua.Product.Name == "LLMConnect");
    }

    // ---------- Timeout ----------

    [Fact]
    public void ConfigureForProvider_SetsTimeoutFromOptions()
    {
        var expectedTimeout = TimeSpan.FromSeconds(45);
        var general = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object,
            Timeout = expectedTimeout
        };
        var client = new HttpClient();

        var configuredClient = HttpClientConfigurator.ConfigureForProvider(general, _defaultEndpointOptions, client);

        configuredClient.Timeout.Should().Be(expectedTimeout);
    }

    // ---------- Header Removal ----------

    [Fact]
    public void ConfigureForProvider_RemovesSpecificHeadersBeforeAdding()
    {
        var client = new HttpClient();

        client.DefaultRequestHeaders.Add("Authorization", "Bearer 1234567890");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("x-api-key", "some-api-key");
        client.DefaultRequestHeaders.Add("anthropic-version", "123");
        client.DefaultRequestHeaders.Add("x-goog-api-key", "some-google-api-key");

        var configuredClient = HttpClientConfigurator.ConfigureForProvider(_defaultGeneralOptions, _defaultEndpointOptions, client);

        configuredClient.DefaultRequestHeaders.Should().NotContainKey("x-goog-api-key");
        configuredClient.DefaultRequestHeaders.Should().NotContainKey("x-api-key");
        configuredClient.DefaultRequestHeaders.Should().NotContainKey("anthropic-version");
        configuredClient.DefaultRequestHeaders.Should().ContainKey("Authorization"); // Should be added back
        configuredClient.DefaultRequestHeaders.GetValues("Authorization").Should().Contain("Bearer test-key");
        configuredClient.DefaultRequestHeaders.Should().ContainKey("Accept");
    }

    // ---------- Unsupported Provider ----------

    [Fact]
    public void ConfigureForProvider_WithUnsupportedProvider_ThrowsNotSupportedException()
    {
        var general = new LLMConnectGeneralOptions
        {
            Provider = (ProviderType)999,
            ApiKey = "test-key",
            LoggerFactory = _loggerFactoryMock.Object,
            Timeout = TimeSpan.FromSeconds(30)
        };
        var client = new HttpClient();

        Action act = () => HttpClientConfigurator.ConfigureForProvider(general, _defaultEndpointOptions, client);

        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Provider '999' is not supported.");
    }
}