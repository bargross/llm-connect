using FluentAssertions;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace LLMConnect.Tests.Streams.ChunkParsers;

public class AnthropicStreamChunkParserTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly LLMConnectClientOptions _options;

    public AnthropicStreamChunkParserTests()
    {
        _loggerMock = new Mock<ILogger>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _options = new LLMConnectClientOptions
        {
            LoggerFactory = loggerFactoryMock.Object
        };
    }

    [Fact]
    public void Parse_WhenEventNameIsContentBlockDeltaAndDataIsValid_ReturnsChatChunk()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var json = @"{""delta"":{""text"":""Hello""}}";
        var evt = new StreamEvent("content_block_delta", json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        result.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void Parse_WhenDataContainsSpecialCharacters_ReturnsCorrectText()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var json = @"{""delta"":{""text"":""Hello\nworld""}}";
        var evt = new StreamEvent("content_block_delta", json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello\nworld");
    }

    [Fact]
    public void Parse_WhenEventNameIsNotContentBlockDelta_ReturnsNull()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var json = @"{""delta"":{""text"":""Hello""}}";
        var evt = new StreamEvent("message_stop", json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        // Logger should not be called (no error)
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WhenDataIsNullOrEmpty_ReturnsNull()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);

        // Act
        var result1 = parser.Parse(new StreamEvent("content_block_delta", null!));
        var result2 = parser.Parse(new StreamEvent("content_block_delta", ""));

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
        // Logger should not be called for empty data
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WhenDataIsDoneSentinel_ReturnsNull()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var evt = new StreamEvent("content_block_delta", "[DONE]");

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WhenDataIsMalformedJson_LogsErrorAndReturnsNull()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var malformedJson = "{delta:{text:Hello}}"; // Invalid JSON (missing quotes)
        var evt = new StreamEvent("content_block_delta", malformedJson);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();

        // Verify logger was called with LogLevel.Information
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ignoring malformed chunks")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Parse_WhenJsonIsValidButDeltaTextIsNull_ReturnsNull()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var json = @"{""delta"":{""text"":null}}";
        var evt = new StreamEvent("content_block_delta", json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        // No log on success
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WhenJsonIsValidButDeltaTextIsEmpty_ReturnsNull()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var json = @"{""delta"":{""text"":""""}}";
        var evt = new StreamEvent("content_block_delta", json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WhenDataIsValidButNotAnthropicFormat_ReturnsNull()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var json = @"{""choices"":[{""delta"":{""content"":""Hello""}}]}"; // OpenAI format
        var evt = new StreamEvent("content_block_delta", json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WhenDataHasExtraFields_IgnoresThem()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var json = @"{""index"":0,""delta"":{""text"":""Hello""},""extra"":""value""}";
        var evt = new StreamEvent("content_block_delta", json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
    }
}