using FluentAssertions;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMConnect.Tests.Streams.ChunkParsers;

public class OllamaStreamChunkParserTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly LLMConnectClientOptions _options;

    public OllamaStreamChunkParserTests()
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
    public void Parse_WhenDataHasContentAndDoneFalse_ReturnsIncompleteChunk()
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
    }

    [Fact]
    public void Parse_WhenDataHasContentAndDoneTrue_ReturnsCompleteChunk()
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
    }

    [Fact]
    public void Parse_WhenDataHasContentAndDoneNull_ReturnsIncompleteChunk()
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
    public void Parse_WhenDataHasContentWithSpecialCharacters_ReturnsCorrectText()
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

    [Fact]
    public void Parse_WhenDataIsNullOrEmpty_ReturnsNull()
    {
        // Arrange
        var parser = new OllamaStreamChunkParser(_options);

        // Act
        var result1 = parser.Parse(new StreamEvent(null, null!));
        var result2 = parser.Parse(new StreamEvent(null, ""));

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();

        // No log should be written
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WhenDataIsMalformedJson_LogsErrorAndReturnsNull()
    {
        // Arrange
        var parser = new OllamaStreamChunkParser(_options);
        var malformedJson = "{message:{content:Hello},done:false}"; // Invalid JSON
        var evt = new StreamEvent(null, malformedJson);

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
    public void Parse_WhenDataHasEmptyContent_ReturnsNull()
    {
        // Arrange
        var parser = new OllamaStreamChunkParser(_options);
        var json = @"{""message"":{""content"":""""},""done"":false}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        // No log should be written
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WhenDataHasNoMessage_ReturnsNull()
    {
        // Arrange
        var parser = new OllamaStreamChunkParser(_options);
        var json = @"{""done"":false}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WhenDataHasMessageButNoContent_ReturnsNull()
    {
        // Arrange
        var parser = new OllamaStreamChunkParser(_options);
        var json = @"{""message"":{},""done"":false}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WhenDataHasExtraFields_IgnoresThem()
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