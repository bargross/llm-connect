using FluentAssertions;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace LLMConnect.Tests.Streams.ChunkParsers;

public class OllamaStreamChunkParserTests
{
    private readonly Mock<ILogger<OllamaStreamChunkParser>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectGeneralOptions _options;

    public OllamaStreamChunkParserTests()
    {
        _loggerMock = new Mock<ILogger<OllamaStreamChunkParser>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _options = new LLMConnectGeneralOptions
        {
            LoggerFactory = _loggerFactoryMock.Object
        };
    }

    // ---------- Content Chunks ----------

    [Fact]
    public void Parse_WithContentAndDoneFalse_ReturnsIncompleteChunk()
    {
        // Arrange
        var parser = new OllamaStreamChunkParser(_options);
        var json = @"{""message"":{""content"":""Hello""},""done"":false}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        result.IsComplete.Should().BeFalse();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WithContentAndDoneTrue_ReturnsCompleteChunk()
    {
        // Arrange
        var parser = new OllamaStreamChunkParser(_options);
        var json = @"{""message"":{""content"":""Hello""},""done"":true}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        result.IsComplete.Should().BeTrue();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WithContentAndDoneNull_ReturnsIncompleteChunk()
    {
        // Arrange
        var parser = new OllamaStreamChunkParser(_options);
        var json = @"{""message"":{""content"":""Hello""}}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        result.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void Parse_WithContentAndSpecialCharacters_ReturnsCorrectText()
    {
        // Arrange
        var parser = new OllamaStreamChunkParser(_options);
        var json = @"{""message"":{""content"":""Hello\nworld""},""done"":false}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello\nworld");
    }

    // ---------- Completion Chunks (No Content) ----------

    [Fact]
    public void Parse_WithDoneTrueAndNoContent_ReturnsCompleteChunkWithEmptyContent()
    {
        // Arrange
        var parser = new OllamaStreamChunkParser(_options);
        var json = @"{""done"":true}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeEmpty();
        result.IsComplete.Should().BeTrue();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WithDoneTrueAndEmptyContent_ReturnsCompleteChunkWithEmptyContent()
    {
        // Arrange
        var parser = new OllamaStreamChunkParser(_options);
        var json = @"{""message"":{""content"":""""},""done"":true}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeEmpty();
        result.IsComplete.Should().BeTrue();
    }

    // ---------- No Content and Not Done ----------

    [Fact]
    public void Parse_WithNoContentAndDoneFalse_ReturnsNull()
    {
        // Arrange
        var parser = new OllamaStreamChunkParser(_options);
        var json = @"{""done"":false}";
        var evt = new StreamEvent(null, json);

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
        var parser = new OllamaStreamChunkParser(_options);
        var evt = new StreamEvent(null, null!);

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
        var parser = new OllamaStreamChunkParser(_options);
        var evt = new StreamEvent(null, "");

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
        var parser = new OllamaStreamChunkParser(_options);
        var malformedJson = "{message:{content:Hello},done:false}";
        var evt = new StreamEvent(null, malformedJson);

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

    // ---------- Extra Fields ----------

    [Fact]
    public void Parse_WithExtraFields_IgnoresThem()
    {
        // Arrange
        var parser = new OllamaStreamChunkParser(_options);
        var json = @"{""extra"":""value"",""message"":{""content"":""Hello""},""done"":false}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        result.IsComplete.Should().BeFalse();
    }
}