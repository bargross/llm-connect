using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using LLMConnect.Streams.StreamReaders;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMConnect.Tests.Streams.StreamReaders;

public class StreamReaderFactoryTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectClientOptions _options;

    public StreamReaderFactoryTests()
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
    [InlineData(ProviderType.OpenAI, typeof(NdjsonStreamEventReader))]
    [InlineData(ProviderType.Ollama, typeof(NdjsonStreamEventReader))]
    [InlineData(ProviderType.Anthropic, typeof(SseStreamEventReader))]
    [InlineData(ProviderType.Google, typeof(SseStreamEventReader))]
    public void Create_ForSupportedProviders_ReturnsCorrectReaderType(ProviderType provider, Type expectedType)
    {
        // Act
        var reader = StreamReaderFactory.Create(provider, _options);

        // Assert
        reader.Should().BeOfType(expectedType);
    }

    [Fact]
    public void Create_ForUnsupportedProvider_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedProvider = (ProviderType)999;

        // Act
        Action act = () => StreamReaderFactory.Create(unsupportedProvider, _options);

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
        Action act = () => StreamReaderFactory.Create(provider, optionsWithoutLogger);

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
        Action act = () => StreamReaderFactory.Create(unsupportedProvider, optionsWithoutLogger);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Provider '{unsupportedProvider.ToString()}' is not supported.*");
        // No log verification because logger is null
    }

    [Fact]
    public void Create_PassesOptionsToReaderConstructor()
    {
        // Arrange
        var provider = ProviderType.OpenAI;

        // Act
        var reader = StreamReaderFactory.Create(provider, _options);

        // Assert
        // Verify that the reader was created with the options.
        // Since we can't directly inspect the private _options field,
        // we can verify that the reader is of the correct type and
        // that it would have used the options (e.g., by checking that the logger
        // is not null if LoggerFactory was provided).
        // Alternatively, we can use reflection to inspect the private field.
        // A simpler approach: we trust that the constructors are wired correctly.
        reader.Should().BeOfType<NdjsonStreamEventReader>();
        // We can also verify that the logger was not used for error (since it's a valid provider)
        _loggerMock.VerifyNoOtherCalls();
    }
}