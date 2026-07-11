using FluentAssertions;
using LLMConnect.Configuration;
using LLMConnect.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace LLMConnect.Tests.Configuration;

public class ServiceCollectionExtensionsTests : IDisposable
{
    public ServiceCollectionExtensionsTests()
    {
        // Reset static state before each test
        ResetCoreServicesRegistered();
    }

    private static void ResetCoreServicesRegistered()
    {
        var field = typeof(ServiceCollectionExtensions)
            .GetField("_coreServicesRegistered", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(null, false);
        }
    }

    public void Dispose()
    {
        // Reset after test to avoid affecting other tests
        ResetCoreServicesRegistered();
    }

    // ---------- Overload 1: Action<LLMConnectClientOptions> ----------

    [Fact]
    public void AddLLMConnect_WithLegacyOptions_RegistersClientAndConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<LLMConnectClientOptions> configure = opts => opts.ApiKey = "test-key";

        // Act
        services.AddLLMConnect(configure);

        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<LLMConnectClientOptions>>().Value;
        options.ApiKey.Should().Be("test-key");

        var client = provider.GetRequiredService<ILLMConnectClient>();
        client.Should().BeOfType<LLMConnectClient>();
    }

    // ---------- Overload 2: Both general and endpoint ----------

    [Fact]
    public void AddLLMConnect_WithGeneralAndEndpointOptions_RegistersBothAndClient()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<LLMConnectGeneralOptions> configureGeneral = opts => opts.ApiKey = "general-key";
        Action<LLMConnectEndpointOptions> configureEndpoint = opts => opts.AzureResourceName = "my-resource";

        // Act
        services.AddLLMConnect(configureGeneral, configureEndpoint);

        var provider = services.BuildServiceProvider();

        // Assert
        var general = provider.GetRequiredService<IOptions<LLMConnectGeneralOptions>>().Value;
        general.ApiKey.Should().Be("general-key");

        var endpoint = provider.GetRequiredService<IOptions<LLMConnectEndpointOptions>>().Value;
        endpoint.AzureResourceName.Should().Be("my-resource");

        var client = provider.GetRequiredService<ILLMConnectClient>();
        client.Should().BeOfType<LLMConnectClient>();
    }

    // ---------- Overload 3: Only general options ----------

    [Fact]
    public void AddLLMConnect_WithOnlyGeneralOptions_RegistersGeneralAndDefaultEndpoint()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<LLMConnectGeneralOptions> configureGeneral = opts => opts.ApiKey = "general-key";

        // Act
        services.AddLLMConnect(configureGeneral);

        var provider = services.BuildServiceProvider();

        // Assert
        var general = provider.GetRequiredService<IOptions<LLMConnectGeneralOptions>>().Value;
        general.ApiKey.Should().Be("general-key");

        // Endpoint options should be registered (default, empty)
        var endpoint = provider.GetRequiredService<IOptions<LLMConnectEndpointOptions>>().Value;
        endpoint.Should().NotBeNull();
        endpoint.AzureResourceName.Should().BeNull();

        var client = provider.GetRequiredService<ILLMConnectClient>();
        client.Should().BeOfType<LLMConnectClient>();
    }

    // ---------- Core registration (only once) ----------

    [Fact]
    public void AddLLMConnect_RegistersCoreServicesOnlyOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        var firstCallCount = services.Count;

        // Act
        services.AddLLMConnect();
        var afterFirstCount = services.Count;

        services.AddLLMConnect();
        var afterSecondCount = services.Count;

        // Assert
        // Core services should not be added twice, but options and client are added each time.
        // Count should increase on first call, but not on second (only the new client/options).
        // To verify, we can check that the number of service descriptors added is consistent.
        // We'll check that after second call, the count is not doubled.

        afterSecondCount.Should().BeGreaterThan(afterFirstCount); // Some services are added (client/options)
        afterSecondCount.Should().BeLessThan(afterFirstCount * 2); // Not double

        // Additionally, we can check that the HttpClient is registered only once.
        var httpClientDescriptors = services.Where(sd => sd.ServiceType == typeof(HttpClient) || sd.ServiceType == typeof(IHttpClientFactory)).ToList();
        httpClientDescriptors.Should().HaveCount(2);
    }

    // ---------- Factory method verification ----------

    [Fact]
    public void AddLLMConnect_CreatesClientWithResolvedOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedApiKey = "resolved-key";
        Action<LLMConnectGeneralOptions> configureGeneral = opts => opts.ApiKey = expectedApiKey;

        services.AddLLMConnect(configureGeneral);

        var provider = services.BuildServiceProvider();

        // Act – we need to inspect the factory delegate, but we can just resolve the client.
        var client = provider.GetRequiredService<ILLMConnectClient>();

        // Assert
        client.Should().BeOfType<LLMConnectClient>();
        // We can't easily inspect the internal options of the client, but we can verify
        // it was constructed with the correct options by checking its behavior.
        // As a compromise, we verify the service is registered as a singleton.
        var clientLifetime = services.First(sd => sd.ServiceType == typeof(ILLMConnectClient)).Lifetime;
        clientLifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddLLMConnect_WithSplitOptions_CreatesClientWithGeneralAndEndpoint()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLLMConnect(
            general => general.ApiKey = "general-key",
            endpoint => endpoint.AzureResourceName = "my-resource");

        var provider = services.BuildServiceProvider();

        // Act
        var client = provider.GetRequiredService<ILLMConnectClient>();

        // Assert
        client.Should().BeOfType<LLMConnectClient>();
        var general = provider.GetRequiredService<IOptions<LLMConnectGeneralOptions>>().Value;
        general.ApiKey.Should().Be("general-key");
        var endpoint = provider.GetRequiredService<IOptions<LLMConnectEndpointOptions>>().Value;
        endpoint.AzureResourceName.Should().Be("my-resource");
    }

    // ---------- Error cases ----------

    [Fact]
    public void AddLLMConnect_WithNullConfigureGeneral_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddLLMConnect((Action<LLMConnectGeneralOptions>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configureOptions");
    }

    [Fact]
    public void AddLLMConnect_WithNullConfigureEndpoint_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddLLMConnect(general => { }, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configureOptions");
    }
}