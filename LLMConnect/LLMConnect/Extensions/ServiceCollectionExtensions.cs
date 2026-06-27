using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LLMConnect.Configuration;

public static class ServiceCollectionExtensions
{
    // 👇 Simple: user provides configuration
    public static IServiceCollection AddLLMConnect(
        this IServiceCollection services,
        Action<LLMClientOptions> configure)
    {
        services.Configure(configure);

        // Register the HttpClientFactory and the provider factory
        services.AddHttpClient("LLMConnect");
        services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();
        services.AddSingleton<ILLMClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LLMClientOptions>>().Value;
            var factory = sp.GetRequiredService<ILLMProviderFactory>();
            return new LLMClient(options, factory);
        });

        return services;
    }

    // 👇 Even simpler: user provides just provider and API key
    public static IServiceCollection AddLLMConnect(
        this IServiceCollection services,
        ProviderType provider,
        string apiKey)
    {
        services.Configure<LLMClientOptions>(options =>
        {
            options.Provider = provider;
            options.ApiKey = apiKey;
        });

        services.AddHttpClient("LLMConnect");
        services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();
        services.AddSingleton<ILLMClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LLMClientOptions>>().Value;
            var factory = sp.GetRequiredService<ILLMProviderFactory>();
            return new LLMClient(options, factory);
        });

        return services;
    }

    // 👇 Advanced: user provides provider, API key, and custom endpoint
    public static IServiceCollection AddLLMConnect(
        this IServiceCollection services,
        ProviderType provider,
        string apiKey,
        string endpoint)
    {
        services.Configure<LLMClientOptions>(options =>
        {
            options.Provider = provider;
            options.ApiKey = apiKey;
            options.Endpoint = endpoint;
        });

        services.AddHttpClient("LLMConnect");
        services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();
        services.AddSingleton<ILLMClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LLMClientOptions>>().Value;
            var factory = sp.GetRequiredService<ILLMProviderFactory>();
            return new LLMClient(options, factory);
        });

        return services;
    }
}