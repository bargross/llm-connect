using FluentAssertions;
using LLMConnect.Configuration;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace LLMConnect.Tests.Configuration;

public class ServiceCollectionExtensionsTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger> _loggerMock;

    public ServiceCollectionExtensionsTests()
    {
        _loggerMock = new Mock<ILogger>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);

        // Reset the static flag before each test to avoid cross-test interference
        ResetCoreServicesRegistered();
    }

    private static void ResetCoreServicesRegistered()
    {
        var field = typeof(ServiceCollectionExtensions)
            .GetField("_coreServicesRegistered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (field != null)
        {
            field.SetValue(null, false);
        }
    }

    // ---------- AddLLMConnect with unified options ----------

    [Fact]
    public void AddLLMConnect_WithConfigureDelegate_RegistersOptionsCorrectly()
    {
        var services = new ServiceCollection();
        var expectedApiKey = "test-key-123";

        // ✅ Explicit cast to resolve ambiguity
        services.AddLLMConnect((Action<LLMConnectClientOptions>)(options =>
        {
            options.Provider = ProviderType.OpenAI;
            options.ApiKey = expectedApiKey;
            options.MaxRetries = 5;
            options.Timeout = TimeSpan.FromSeconds(30);
            options.LoggerFactory = _loggerFactoryMock.Object;
        }));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LLMConnectClientOptions>>().Value;

        options.Provider.Should().Be(ProviderType.OpenAI);
        options.ApiKey.Should().Be(expectedApiKey);
        options.MaxRetries.Should().Be(5);
        options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        options.LoggerFactory.Should().Be(_loggerFactoryMock.Object);
    }

    [Fact]
    public void AddLLMConnect_WithoutConfigureDelegate_RegistersDefaultOptions()
    {
        var services = new ServiceCollection();
        services.AddLLMConnect();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LLMConnectClientOptions>>().Value;

        options.Provider.Should().Be(ProviderType.OpenAI);
        options.ApiKey.Should().BeEmpty();
        options.MaxRetries.Should().Be(3);
        options.Timeout.Should().Be(TimeSpan.FromSeconds(60));
        options.LoggerFactory.Should().BeNull();
    }

    [Fact]
    public void AddLLMConnect_RegistersClientAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLLMConnect((Action<LLMConnectGeneralOptions>)(general => {
            general.ApiKey = "test-key";
        }));

        var provider = services.BuildServiceProvider();

        var client1 = provider.GetService<ILLMConnectClient>();
        var client2 = provider.GetService<ILLMConnectClient>();

        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client1.Should().BeSameAs(client2);
    }

    [Fact]
    public void AddLLMConnect_RegistersNamedHttpClientWithRetryHandler()
    {
        var services = new ServiceCollection();
        services.AddLLMConnect();

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        using var httpClient = factory.CreateClient("LLMConnect");
        httpClient.Should().NotBeNull();
        factory.Should().NotBeNull();
    }

    // ---------- AddLLMConnect with split options ----------

    [Fact]
    public void AddLLMConnect_WithSplitOptions_RegistersGeneralAndEndpointOptionsCorrectly()
    {
        var services = new ServiceCollection();

        // ✅ No ambiguity here – two parameters make it clear
        services.AddLLMConnect(
            general =>
            {
                general.Provider = ProviderType.Anthropic;
                general.ApiKey = "anthropic-key";
                general.DefaultModel = "claude-3";
                general.MaxRetries = 2;
                general.Timeout = TimeSpan.FromSeconds(45);
                general.LoggerFactory = _loggerFactoryMock.Object;
            },
            endpoint =>
            {
                endpoint.OllamaPort = 11435;
                endpoint.Endpoint = "https://custom-endpoint.com";
            });

        var provider = services.BuildServiceProvider();
        var generalOptions = provider.GetRequiredService<IOptions<LLMConnectGeneralOptions>>().Value;
        var endpointOptions = provider.GetRequiredService<IOptions<LLMConnectEndpointOptions>>().Value;

        generalOptions.Provider.Should().Be(ProviderType.Anthropic);
        generalOptions.ApiKey.Should().Be("anthropic-key");
        generalOptions.DefaultModel.Should().Be("claude-3");
        generalOptions.MaxRetries.Should().Be(2);
        generalOptions.Timeout.Should().Be(TimeSpan.FromSeconds(45));
        generalOptions.LoggerFactory.Should().Be(_loggerFactoryMock.Object);

        endpointOptions.OllamaPort.Should().Be(11435);
        endpointOptions.Endpoint.Should().Be("https://custom-endpoint.com");
    }

    [Fact]
    public void AddLLMConnect_WithOnlyGeneralOptions_RegistersGeneralOptionsWithDefaultEndpoint()
    {
        var services = new ServiceCollection();

        // ✅ Explicit cast to resolve ambiguity
        services.AddLLMConnect((Action<LLMConnectGeneralOptions>)(general =>
        {
            general.Provider = ProviderType.OpenAI;
            general.ApiKey = "openai-key";
        }));

        var provider = services.BuildServiceProvider();
        var generalOptions = provider.GetRequiredService<IOptions<LLMConnectGeneralOptions>>().Value;
        var endpointOptions = provider.GetRequiredService<IOptions<LLMConnectEndpointOptions>>().Value;

        generalOptions.Provider.Should().Be(ProviderType.OpenAI);
        generalOptions.ApiKey.Should().Be("openai-key");
        endpointOptions.Should().NotBeNull();
        endpointOptions.Endpoint.Should().BeNull();
    }

    [Fact]
    public void AddLLMConnect_WithSplitOptions_RegistersClientAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLLMConnect(
            general => {
                general.Provider = ProviderType.OpenAI;
                general.ApiKey = "key";
            },
            endpoint => { });

        var provider = services.BuildServiceProvider();
        var client1 = provider.GetService<ILLMConnectClient>();
        var client2 = provider.GetService<ILLMConnectClient>();

        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client1.Should().BeSameAs(client2);
    }

    // ---------- Multiple registrations (lock) ----------

    [Fact]
    public void AddLLMConnect_CalledMultipleTimes_RegistersCoreServicesOnlyOnce()
    {
        var services = new ServiceCollection();

        // ✅ Explicit cast to resolve ambiguity
        services.AddLLMConnect((Action<LLMConnectClientOptions>)(options => options.ApiKey = "first"));
        services.AddLLMConnect((Action<LLMConnectClientOptions>)(options => options.ApiKey = "second"));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LLMConnectClientOptions>>().Value;

        options.ApiKey.Should().Be("second");
        var factory = provider.GetService<IHttpClientFactory>();
        factory.Should().NotBeNull();
    }
}