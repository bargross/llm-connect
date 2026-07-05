using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Xunit;

namespace LLMConnect.Tests.MappingExtensions;

public class LLMConnectClientOptionsExtensionsTests
{
    // ---------- InternalComputedDefaultModel ----------

    [Theory]
    [InlineData(ProviderType.OpenAI, "gpt-3.5-turbo")]
    [InlineData(ProviderType.Anthropic, "claude-3-5-sonnet-20241022")]
    [InlineData(ProviderType.Google, "gemini-2.0-flash")]
    [InlineData(ProviderType.Ollama, "llama3.2")]
    public void InternalComputedDefaultModel_WhenNoModelOrDefault_ReturnsProviderDefault(ProviderType provider, string expectedDefault)
    {
        // Arrange
        var options = new LLMConnectGeneralOptions { Provider = provider };

        // Act
        var result = options.InternalComputedDefaultModel();

        // Assert
        result.Should().Be(expectedDefault);
    }

    [Fact]
    public void InternalComputedDefaultModel_WhenModelProvided_ReturnsModel()
    {
        // Arrange
        var options = new LLMConnectGeneralOptions { Provider = ProviderType.OpenAI };
        var providedModel = "custom-model";

        // Act
        var result = options.InternalComputedDefaultModel(providedModel);

        // Assert
        result.Should().Be(providedModel);
    }

    [Fact]
    public void InternalComputedDefaultModel_WhenDefaultModelSet_ReturnsDefaultModel()
    {
        // Arrange
        var defaultModel = "my-default-model";
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            DefaultModel = defaultModel
        };

        // Act
        var result = options.InternalComputedDefaultModel();

        // Assert
        result.Should().Be(defaultModel);
    }

    [Fact]
    public void InternalComputedDefaultModel_WhenModelAndDefaultProvided_ModelTakesPrecedence()
    {
        // Arrange
        var model = "explicit-model";
        var defaultModel = "default-model";
        var options = new LLMConnectGeneralOptions
        {
            Provider = ProviderType.OpenAI,
            DefaultModel = defaultModel
        };

        // Act
        var result = options.InternalComputedDefaultModel(model);

        // Assert
        result.Should().Be(model);
    }

    [Fact]
    public void InternalComputedDefaultModel_ForUnsupportedProvider_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedProvider = (ProviderType)999;
        var options = new LLMConnectGeneralOptions { Provider = unsupportedProvider };

        // Act
        Action act = () => options.InternalComputedDefaultModel();

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Provider '{unsupportedProvider}' is not supported.*");
    }

    // ---------- ToGeneralOptions ----------

    [Fact]
    public void ToGeneralOptions_WhenOptionsIsNull_ReturnsDefaultGeneralOptions()
    {
        // Act
        var result = LLMConnectClientOptionsExtensions.ToGeneralOptions(null!);

        // Assert
        result.Should().NotBeNull();
        result.Provider.Should().Be(ProviderType.OpenAI);
        result.ApiKey.Should().BeEmpty();
        result.DefaultModel.Should().BeNull();
        result.Timeout.Should().Be(TimeSpan.FromSeconds(60));
        result.MaxRetries.Should().Be(3);
        result.LoggerFactory.Should().BeNull();
    }

    [Fact]
    public void ToGeneralOptions_WhenOptionsIsValid_MapsAllProperties()
    {
        // Arrange
        var original = new LLMConnectClientOptions
        {
            Provider = ProviderType.Anthropic,
            ApiKey = "test-key",
            DefaultModel = "claude-3",
            Timeout = TimeSpan.FromSeconds(99),
            MaxRetries = 5,
            LoggerFactory = null // cannot easily create one, but it's a reference type
        };

        // Act
        var result = original.ToGeneralOptions();

        // Assert
        result.Provider.Should().Be(original.Provider);
        result.ApiKey.Should().Be(original.ApiKey);
        result.DefaultModel.Should().Be(original.DefaultModel);
        result.Timeout.Should().Be(original.Timeout);
        result.MaxRetries.Should().Be(original.MaxRetries);
        result.LoggerFactory.Should().Be(original.LoggerFactory);
    }

    // ---------- ToEndpointOptions ----------

    [Fact]
    public void ToEndpointOptions_WhenOptionsIsNull_ReturnsDefaultEndpointOptions()
    {
        // Act
        var result = LLMConnectClientOptionsExtensions.ToEndpointOptions(null!);

        // Assert
        result.Should().NotBeNull();
        result.Endpoint.Should().BeNull();
        result.OllamaPort.Should().BeNull();
        result.ChatResponseDeserializer.Should().BeNull();
        result.EmbeddingResponseDeserializer.Should().BeNull();
        result.CustomStreamEventReaderFactory.Should().BeNull();
        result.CustomStreamChunkParserFactory.Should().BeNull();
        result.ExtraOptions.Should().BeNull();
    }

    [Fact]
    public void ToEndpointOptions_WhenOptionsIsValid_MapsAllProperties()
    {
        // Arrange
        Func<string, CancellationToken, Task<ChatResponse>> chatDeserializer = (json, ct) => Task.FromResult(new ChatResponse());
        Func<string, CancellationToken, Task<EmbeddingResponse>> embedDeserializer = (json, ct) => Task.FromResult(new EmbeddingResponse());
        Func<IStreamEventReader> readerFactory = () => null!;
        Func<IStreamChunkParser> parserFactory = () => null!;
        var extraOptions = new Dictionary<string, object> { { "key", "value" } };

        var original = new LLMConnectClientOptions
        {
            Endpoint = "https://custom.endpoint",
            OllamaPort = 11435,
            ChatResponseDeserializer = chatDeserializer,
            EmbeddingResponseDeserializer = embedDeserializer,
            CustomStreamEventReaderFactory = readerFactory,
            CustomStreamChunkParserFactory = parserFactory,
            ExtraOptions = extraOptions
        };

        // Act
        var result = original.ToEndpointOptions();

        // Assert
        result.Endpoint.Should().Be(original.Endpoint);
        result.OllamaPort.Should().Be(original.OllamaPort);
        result.ChatResponseDeserializer.Should().BeSameAs(original.ChatResponseDeserializer);
        result.EmbeddingResponseDeserializer.Should().BeSameAs(original.EmbeddingResponseDeserializer);
        result.CustomStreamEventReaderFactory.Should().BeSameAs(original.CustomStreamEventReaderFactory);
        result.CustomStreamChunkParserFactory.Should().BeSameAs(original.CustomStreamChunkParserFactory);
        result.ExtraOptions.Should().BeSameAs(original.ExtraOptions);
    }
}