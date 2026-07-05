using FluentAssertions;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace LLMConnect.Tests.Streams.ChunkParsers;

public class AnthropicStreamChunkParserTests
{
    private readonly Mock<ILogger<AnthropicStreamChunkParser>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectGeneralOptions _options;

    public AnthropicStreamChunkParserTests()
    {
        _loggerMock = new Mock<ILogger<AnthropicStreamChunkParser>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _options = new LLMConnectGeneralOptions
        {
            LoggerFactory = _loggerFactoryMock.Object
        };
    }

    // ---------- Valid Events ----------

    [Fact]
    public void Parse_WithValidContentBlockDelta_ReturnsChatChunk()
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
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WithValidContentBlockDeltaAndSpecialCharacters_ReturnsCorrectText()
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

    // ---------- Invalid Event Names ----------

    [Fact]
    public void Parse_WithNonContentBlockDeltaEvent_ReturnsNull()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var json = @"{""delta"":{""text"":""Hello""}}";
        var evt = new StreamEvent("message_stop", json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Empty or Null Data ----------

    [Fact]
    public void Parse_WithNullData_ReturnsNull()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var evt = new StreamEvent("content_block_delta", null!);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WithEmptyData_ReturnsNull()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var evt = new StreamEvent("content_block_delta", "");

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WithDoneSentinel_ReturnsNull()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var evt = new StreamEvent("content_block_delta", "[DONE]");

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Malformed JSON ----------

    [Fact]
    public void Parse_WithMalformedJson_LogsAndReturnsNull()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var malformedJson = "{delta:{text:Hello}}"; // Missing quotes
        var evt = new StreamEvent("content_block_delta", malformedJson);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ignoring malformed chunks")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ---------- Valid JSON but Empty Text ----------

    [Fact]
    public void Parse_WithValidJsonButEmptyText_ReturnsNull()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var json = @"{""delta"":{""text"":""""}}";
        var evt = new StreamEvent("content_block_delta", json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WithValidJsonButNullText_ReturnsNull()
    {
        // Arrange
        var parser = new AnthropicStreamChunkParser(_options);
        var json = @"{""delta"":{""text"":null}}";
        var evt = new StreamEvent("content_block_delta", json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Extra Fields ----------

    [Fact]
    public void Parse_WithExtraFields_IgnoresThem()
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
        _loggerMock.VerifyNoOtherCalls();
    }
}