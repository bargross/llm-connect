using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMConnect.Tests.Streams.ChunkParsers;

public class StreamChunkParserFactoryTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectGeneralOptions _options;

    public StreamChunkParserFactoryTests()
    {
        _loggerMock = new Mock<ILogger>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _options = new LLMConnectGeneralOptions
        {
            LoggerFactory = _loggerFactoryMock.Object
        };
    }

    // ---------- Supported Providers ----------

    [Theory]
    [InlineData(ProviderType.OpenAI, typeof(OpenAIStreamChunkParser))]
    [InlineData(ProviderType.Anthropic, typeof(AnthropicStreamChunkParser))]
    [InlineData(ProviderType.Google, typeof(GoogleStreamChunkParser))]
    [InlineData(ProviderType.Ollama, typeof(OllamaStreamChunkParser))]
    public void Create_ForSupportedProvider_ReturnsCorrectParserType(ProviderType provider, Type expectedType)
    {
        // Act
        var parser = StreamChunkParserFactory.Create(provider, _options);

        // Assert
        parser.Should().BeOfType(expectedType);
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Unsupported Provider ----------

    [Fact]
    public void Create_ForUnsupportedProvider_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedProvider = (ProviderType)999;

        // Act
        Action act = () => StreamChunkParserFactory.Create(unsupportedProvider, _options);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Provider '{unsupportedProvider.ToString()}' is not supported.*");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Provider '{unsupportedProvider.ToString()}' is not supported.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ---------- Null Logger Factory ----------

    [Fact]
    public void Create_WhenLoggerFactoryIsNull_DoesNotThrow()
    {
        // Arrange
        var optionsWithoutLogger = new LLMConnectGeneralOptions
        {
            LoggerFactory = null
        };

        // Act
        Action act = () => StreamChunkParserFactory.Create(ProviderType.OpenAI, optionsWithoutLogger);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_WhenUnsupportedProviderAndLoggerFactoryIsNull_StillThrows()
    {
        // Arrange
        var optionsWithoutLogger = new LLMConnectGeneralOptions
        {
            LoggerFactory = null
        };
        var unsupportedProvider = (ProviderType)999;

        // Act
        Action act = () => StreamChunkParserFactory.Create(unsupportedProvider, optionsWithoutLogger);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Provider '{unsupportedProvider.ToString()}' is not supported.*");
        // No log verification because logger is null
    }

    // ---------- Provider-Specific Parser Creation ----------

    [Fact]
    public void Create_ForOpenAI_ReturnsOpenAIStreamChunkParser()
    {
        var parser = StreamChunkParserFactory.Create(ProviderType.OpenAI, _options);
        parser.Should().BeOfType<OpenAIStreamChunkParser>();
        parser.Should().BeAssignableTo<IStreamChunkParser>();
    }

    [Fact]
    public void Create_ForAnthropic_ReturnsAnthropicStreamChunkParser()
    {
        var parser = StreamChunkParserFactory.Create(ProviderType.Anthropic, _options);
        parser.Should().BeOfType<AnthropicStreamChunkParser>();
        parser.Should().BeAssignableTo<IStreamChunkParser>();
    }

    [Fact]
    public void Create_ForGoogle_ReturnsGoogleStreamChunkParser()
    {
        var parser = StreamChunkParserFactory.Create(ProviderType.Google, _options);
        parser.Should().BeOfType<GoogleStreamChunkParser>();
        parser.Should().BeAssignableTo<IStreamChunkParser>();
    }

    [Fact]
    public void Create_ForOllama_ReturnsOllamaStreamChunkParser()
    {
        var parser = StreamChunkParserFactory.Create(ProviderType.Ollama, _options);
        parser.Should().BeOfType<OllamaStreamChunkParser>();
        parser.Should().BeAssignableTo<IStreamChunkParser>();
    }
}