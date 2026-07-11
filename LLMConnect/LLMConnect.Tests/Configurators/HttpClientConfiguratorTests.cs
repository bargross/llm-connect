using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using Moq;

namespace LLMConnect.Tests.Infrastructure;

public class HttpClientConfiguratorTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger> _loggerMock;

    public HttpClientConfiguratorTests()
    {
        _loggerMock = new Mock<ILogger>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);
    }

    private LLMConnectGeneralOptions CreateGeneralOptions(ProviderType provider, string apiKey = "test-key", TimeSpan? timeout = null)
    {
        return new LLMConnectGeneralOptions
        {
            Provider = provider,
            ApiKey = apiKey,
            LoggerFactory = _loggerFactoryMock.Object,
            Timeout = timeout ?? TimeSpan.FromSeconds(30),
            DefaultModel = provider == ProviderType.Ollama ? "llama3" : "gpt-4"
        };
    }

    private LLMConnectEndpointOptions CreateEndpointOptions(
        int? ollamaPort = null,
        string? azureResource = null,
        string? azureDeployment = null,
        string? azureApiVersion = null)
    {
        return new LLMConnectEndpointOptions
        {
            OllamaPort = ollamaPort,
            AzureResourceName = azureResource,
            AzureDeploymentName = azureDeployment,
            AzureApiVersion = azureApiVersion
        };
    }

    // ---------- Base Address ----------

    [Fact]
    public void ConfigureForProvider_ForOpenAI_SetsCorrectBaseAddress()
    {
        // Arrange
        var generalOpts = CreateGeneralOptions(ProviderType.OpenAI);
        var endpointOpts = CreateEndpointOptions();
        var client = new HttpClient();

        // Act
        var configured = HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        // Assert
        configured.BaseAddress.Should().NotBeNull();
        configured.BaseAddress!.ToString().Should().Be("https://api.openai.com/v1/");
    }

    [Fact]
    public void ConfigureForProvider_ForAnthropic_SetsCorrectBaseAddress()
    {
        var generalOpts = CreateGeneralOptions(ProviderType.Anthropic);
        var endpointOpts = CreateEndpointOptions();
        var client = new HttpClient();

        var configured = HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        configured.BaseAddress.Should().NotBeNull();
        configured.BaseAddress!.ToString().Should().Be("https://api.anthropic.com/v1/");
    }

    [Fact]
    public void ConfigureForProvider_ForGoogle_SetsCorrectBaseAddress()
    {
        var generalOpts = CreateGeneralOptions(ProviderType.Google);
        var endpointOpts = CreateEndpointOptions();
        var client = new HttpClient();

        var configured = HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        configured.BaseAddress.Should().NotBeNull();
        configured.BaseAddress!.ToString().Should().Be("https://generativelanguage.googleapis.com/v1beta/");
    }

    [Fact]
    public void ConfigureForProvider_ForOllama_SetsCorrectBaseAddressWithDefaultPort()
    {
        var generalOpts = CreateGeneralOptions(ProviderType.Ollama);
        var endpointOpts = CreateEndpointOptions();
        var client = new HttpClient();

        var configured = HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        configured.BaseAddress.Should().NotBeNull();
        configured.BaseAddress!.ToString().Should().Be("http://localhost:11434/");
    }

    [Fact]
    public void ConfigureForProvider_ForOllama_WithCustomPort_SetsCorrectBaseAddress()
    {
        var generalOpts = CreateGeneralOptions(ProviderType.Ollama);
        var endpointOpts = CreateEndpointOptions(ollamaPort: 12345);
        var client = new HttpClient();

        var configured = HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        configured.BaseAddress.Should().NotBeNull();
        configured.BaseAddress!.ToString().Should().Be("http://localhost:12345/");
    }

    [Fact]
    public void ConfigureForProvider_ForAzureOpenAI_SetsCorrectBaseAddress()
    {
        var generalOpts = CreateGeneralOptions(ProviderType.AzureOpenAI);
        var endpointOpts = CreateEndpointOptions(
            azureResource: "my-resource",
            azureDeployment: "my-deployment",
            azureApiVersion: "2024-02-15-preview");
        var client = new HttpClient();

        var configured = HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        configured.BaseAddress.Should().NotBeNull();
        configured.BaseAddress!.ToString().Should().Be("https://my-resource.openai.azure.com/openai/deployments/my-deployment/");
    }

    // ---------- Timeout ----------

    [Fact]
    public void ConfigureForProvider_SetsTimeoutFromGeneralOptions()
    {
        var expectedTimeout = TimeSpan.FromSeconds(45);
        var generalOpts = CreateGeneralOptions(ProviderType.OpenAI, timeout: expectedTimeout);
        var endpointOpts = CreateEndpointOptions();
        var client = new HttpClient();

        var configured = HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        configured.Timeout.Should().Be(expectedTimeout);
    }

    // ---------- Headers ----------

    [Fact]
    public void ConfigureForProvider_ForOpenAI_SetsAuthorizationBearerAndAccept()
    {
        var generalOpts = CreateGeneralOptions(ProviderType.OpenAI, apiKey: "openai-key");
        var endpointOpts = CreateEndpointOptions();
        var client = new HttpClient();

        var configured = HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        configured.DefaultRequestHeaders.Should().ContainKey("Authorization");
        configured.DefaultRequestHeaders.GetValues("Authorization").Should().Contain("Bearer openai-key");
        configured.DefaultRequestHeaders.Should().ContainKey("Accept");
        configured.DefaultRequestHeaders.GetValues("Accept").Should().Contain("application/json");
    }

    [Fact]
    public void ConfigureForProvider_ForAzureOpenAI_SetsApiKeyHeaderAndAccept()
    {
        // Arrange
        var generalOpts = CreateGeneralOptions(ProviderType.AzureOpenAI, apiKey: "azure-key");
        var endpointOpts = new LLMConnectEndpointOptions
        {
            AzureResourceName = "my-resource",
            AzureDeploymentName = "my-deployment",
            AzureApiVersion = "2024-02-15-preview"
        };
        var client = new HttpClient();

        // Act
        var configured = HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        // Assert
        configured.DefaultRequestHeaders.Should().ContainKey("api-key");
        configured.DefaultRequestHeaders.GetValues("api-key").Should().Contain("azure-key");
        configured.DefaultRequestHeaders.Should().ContainKey("Accept");
        configured.DefaultRequestHeaders.GetValues("Accept").Should().Contain("application/json");
    }

    [Fact]
    public void ConfigureForProvider_ForAnthropic_SetsXApiKeyAndAnthropicVersionAndAccept()
    {
        var generalOpts = CreateGeneralOptions(ProviderType.Anthropic, apiKey: "anthropic-key");
        var endpointOpts = CreateEndpointOptions();
        var client = new HttpClient();

        var configured = HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        configured.DefaultRequestHeaders.Should().ContainKey("x-api-key");
        configured.DefaultRequestHeaders.GetValues("x-api-key").Should().Contain("anthropic-key");
        configured.DefaultRequestHeaders.Should().ContainKey("anthropic-version");
        configured.DefaultRequestHeaders.GetValues("anthropic-version").Should().Contain("2023-06-01");
        configured.DefaultRequestHeaders.Should().ContainKey("Accept");
        configured.DefaultRequestHeaders.GetValues("Accept").Should().Contain("application/json");
    }

    [Fact]
    public void ConfigureForProvider_ForGoogle_SetsXGoogApiKeyAndAccept()
    {
        var generalOpts = CreateGeneralOptions(ProviderType.Google, apiKey: "google-key");
        var endpointOpts = CreateEndpointOptions();
        var client = new HttpClient();

        var configured = HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        configured.DefaultRequestHeaders.Should().ContainKey("x-goog-api-key");
        configured.DefaultRequestHeaders.GetValues("x-goog-api-key").Should().Contain("google-key");
        configured.DefaultRequestHeaders.Should().ContainKey("Accept");
        configured.DefaultRequestHeaders.GetValues("Accept").Should().Contain("application/json");
    }

    [Fact]
    public void ConfigureForProvider_ForOllama_SetsNoAuthenticationHeaders()
    {
        var generalOpts = CreateGeneralOptions(ProviderType.Ollama);
        var endpointOpts = CreateEndpointOptions();
        var client = new HttpClient();

        var configured = HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        configured.DefaultRequestHeaders.Should().NotContainKey("Authorization");
        configured.DefaultRequestHeaders.Should().NotContainKey("api-key");
        configured.DefaultRequestHeaders.Should().NotContainKey("x-api-key");
        configured.DefaultRequestHeaders.Should().NotContainKey("x-goog-api-key");
        configured.DefaultRequestHeaders.Should().NotContainKey("anthropic-version");
        // Accept is not added for Ollama
        configured.DefaultRequestHeaders.Should().NotContainKey("Accept");
    }

    // ---------- Header Removal ----------

    [Fact]
    public void ConfigureForProvider_RemovesControlledHeadersBeforeAdding()
    {
        var generalOpts = CreateGeneralOptions(ProviderType.OpenAI, apiKey: "openai-key");
        var endpointOpts = CreateEndpointOptions();
        var client = new HttpClient();

        // Pre-populate headers that should be removed
        client.DefaultRequestHeaders.Add("Authorization", "Bearer old-token");
        client.DefaultRequestHeaders.Add("Accept", "text/plain");
        client.DefaultRequestHeaders.Add("x-api-key", "old-key");
        client.DefaultRequestHeaders.Add("anthropic-version", "old-version");
        client.DefaultRequestHeaders.Add("x-goog-api-key", "old-google-key");

        var configured = HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        // All controlled headers should be removed and replaced with correct ones
        configured.DefaultRequestHeaders.Should().ContainKey("Authorization");
        configured.DefaultRequestHeaders.GetValues("Authorization").Should().Contain("Bearer openai-key");
        configured.DefaultRequestHeaders.Should().ContainKey("Accept");
        configured.DefaultRequestHeaders.GetValues("Accept").Should().Contain("application/json");
        configured.DefaultRequestHeaders.Should().NotContainKey("x-api-key");
        configured.DefaultRequestHeaders.Should().NotContainKey("anthropic-version");
        configured.DefaultRequestHeaders.Should().NotContainKey("x-goog-api-key");
    }

    // ---------- User-Agent ----------

    [Fact]
    public void ConfigureForProvider_AddsDefaultUserAgent_WhenNonePresent()
    {
        var generalOpts = CreateGeneralOptions(ProviderType.OpenAI);
        var endpointOpts = CreateEndpointOptions();
        var client = new HttpClient();

        var configured = HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        configured.DefaultRequestHeaders.UserAgent.Should().ContainSingle(ua => ua.Product.Name == "LLMConnect");
    }

    [Fact]
    public void ConfigureForProvider_DoesNotAddDefaultUserAgent_WhenUserAgentAlreadyPresent()
    {
        var generalOpts = CreateGeneralOptions(ProviderType.OpenAI);
        var endpointOpts = CreateEndpointOptions();
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CustomAgent", "1.0"));

        var configured = HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        configured.DefaultRequestHeaders.UserAgent.Should().ContainSingle(ua => ua.Product.Name == "CustomAgent");
        configured.DefaultRequestHeaders.UserAgent.Should().NotContain(ua => ua.Product.Name == "LLMConnect");
    }

    // ---------- Unsupported Provider ----------

    [Fact]
    public void ConfigureForProvider_WithUnsupportedProvider_ThrowsNotSupportedException()
    {
        var unsupportedProvider = (ProviderType)999;
        var generalOpts = CreateGeneralOptions(unsupportedProvider);
        var endpointOpts = CreateEndpointOptions();
        var client = new HttpClient();

        Action act = () => HttpClientConfigurator.ConfigureForProvider(generalOpts, endpointOpts, client);

        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Provider '{unsupportedProvider}' is not supported.");
    }
}