using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMConnect.Tests.Streams.StreamReaders;

public class StreamReaderFactoryTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectGeneralOptions _options;

    public StreamReaderFactoryTests()
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
    [InlineData(ProviderType.OpenAI, typeof(NdjsonStreamEventReader))]
    [InlineData(ProviderType.Anthropic, typeof(SseStreamEventReader))]
    [InlineData(ProviderType.Google, typeof(SseStreamEventReader))]
    [InlineData(ProviderType.Ollama, typeof(NdjsonStreamEventReader))]
    public void Create_ForSupportedProvider_ReturnsCorrectReaderType(ProviderType provider, Type expectedType)
    {
        // Act
        var reader = StreamReaderFactory.Create(provider, _options);

        // Assert
        reader.Should().BeOfType(expectedType);
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Unsupported Provider ----------

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
        Action act = () => StreamReaderFactory.Create(ProviderType.OpenAI, optionsWithoutLogger);

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
        Action act = () => StreamReaderFactory.Create(unsupportedProvider, optionsWithoutLogger);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Provider '{unsupportedProvider.ToString()}' is not supported.*");
        // No log verification because logger is null
    }

    // ---------- Provider-Specific Reader Creation ----------

    [Fact]
    public void Create_ForOpenAI_ReturnsNdjsonStreamEventReader()
    {
        var reader = StreamReaderFactory.Create(ProviderType.OpenAI, _options);
        reader.Should().BeOfType<NdjsonStreamEventReader>();
        reader.Should().BeAssignableTo<IStreamEventReader>();
    }

    [Fact]
    public void Create_ForAnthropic_ReturnsSseStreamEventReader()
    {
        var reader = StreamReaderFactory.Create(ProviderType.Anthropic, _options);
        reader.Should().BeOfType<SseStreamEventReader>();
        reader.Should().BeAssignableTo<IStreamEventReader>();
    }

    [Fact]
    public void Create_ForGoogle_ReturnsSseStreamEventReader()
    {
        var reader = StreamReaderFactory.Create(ProviderType.Google, _options);
        reader.Should().BeOfType<SseStreamEventReader>();
        reader.Should().BeAssignableTo<IStreamEventReader>();
    }

    [Fact]
    public void Create_ForOllama_ReturnsNdjsonStreamEventReader()
    {
        var reader = StreamReaderFactory.Create(ProviderType.Ollama, _options);
        reader.Should().BeOfType<NdjsonStreamEventReader>();
        reader.Should().BeAssignableTo<IStreamEventReader>();
    }
}