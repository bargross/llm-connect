using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMConnect.Tests.Streams.ChunkParsers;

public class StreamChunkParserFactoryTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectClientOptions _options;

    public StreamChunkParserFactoryTests()
    {
        _loggerMock = new Mock<ILogger>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _options = new LLMConnectClientOptions
        {
            LoggerFactory = _loggerFactoryMock.Object
        };
    }

    [Theory]
    [InlineData(ProviderType.OpenAI, typeof(OpenAIStreamChunkParser))]
    [InlineData(ProviderType.Anthropic, typeof(AnthropicStreamChunkParser))]
    [InlineData(ProviderType.Google, typeof(GoogleStreamChunkParser))]
    [InlineData(ProviderType.Ollama, typeof(OllamaStreamChunkParser))]
    public void Create_ForSupportedProviders_ReturnsCorrectParserType(ProviderType provider, Type expectedType)
    {
        // Act
        var parser = StreamChunkParserFactory.Create(provider, _options);

        // Assert
        parser.Should().BeOfType(expectedType);
    }

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

        // Verify logger was called with error
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Provider '{unsupportedProvider.ToString()}' is not supported.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Create_WhenLoggerFactoryIsNull_DoesNotThrow()
    {
        // Arrange
        var optionsWithoutLogger = new LLMConnectClientOptions
        {
            LoggerFactory = null
        };
        var provider = ProviderType.OpenAI;

        // Act
        Action act = () => StreamChunkParserFactory.Create(provider, optionsWithoutLogger);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_WhenUnsupportedProviderAndLoggerFactoryIsNull_StillThrows()
    {
        // Arrange
        var optionsWithoutLogger = new LLMConnectClientOptions
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

    [Fact]
    public void Create_PassesOptionsToParserConstructor()
    {
        // Arrange
        var provider = ProviderType.OpenAI;

        // Act
        var parser = StreamChunkParserFactory.Create(provider, _options);

        // Assert
        parser.Should().BeOfType<OpenAIStreamChunkParser>();
        // We can verify that the logger was not used for error (since it's a valid provider)
        _loggerMock.VerifyNoOtherCalls();
    }
}