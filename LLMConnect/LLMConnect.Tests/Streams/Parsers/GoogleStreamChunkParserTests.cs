using FluentAssertions;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMConnect.Tests.Streams.ChunkParsers;

public class GoogleStreamChunkParserTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly LLMConnectClientOptions _options;

    public GoogleStreamChunkParserTests()
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
    public void Parse_WhenDataContainsText_ReturnsChatChunk()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{""candidates"":[{""content"":{""parts"":[{""text"":""Hello""}]}}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        result.IsComplete.Should().BeFalse();
        result.FinishReason.Should().BeNull();
    }

    [Fact]
    public void Parse_WhenDataContainsTextWithSpecialCharacters_ReturnsCorrectText()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{""candidates"":[{""content"":{""parts"":[{""text"":""Hello\nworld""}]}}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello\nworld");
    }

    [Fact]
    public void Parse_WhenDataContainsFinishReason_ReturnsCompleteChunk()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{""candidates"":[{""finishReason"":""STOP""}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeEmpty();
        result.IsComplete.Should().BeTrue();
        result.FinishReason.Should().Be("STOP");
    }

    [Fact]
    public void Parse_WhenDataContainsFinishReasonWithDifferentValue_ReturnsCorrectReason()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{""candidates"":[{""finishReason"":""MAX_TOKENS""}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.IsComplete.Should().BeTrue();
        result.FinishReason.Should().Be("MAX_TOKENS");
    }

    [Fact]
    public void Parse_WhenDataContainsBothTextAndFinishReason_TextTakesPriority()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{""candidates"":[{""content"":{""parts"":[{""text"":""Hello""}]},""finishReason"":""STOP""}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        result.IsComplete.Should().BeFalse(); // Should not mark as complete yet
        result.FinishReason.Should().BeNull();
    }

    [Fact]
    public void Parse_WhenDataContainsUsageMetadataOnly_ReturnsNull()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{""usageMetadata"":{""promptTokenCount"":10,""candidatesTokenCount"":5}}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        // No log should be written
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WhenDataIsNullOrEmpty_ReturnsNull()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);

        // Act
        var result1 = parser.Parse(new StreamEvent(null, null!));
        var result2 = parser.Parse(new StreamEvent(null, ""));
        var result3 = parser.Parse(new StreamEvent(null, " "));

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
        result3.Should().BeNull();
        // No log should be written
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WhenDataIsMalformedJson_LogsErrorAndReturnsNull()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var malformedJson = "{candidates:[{content:{parts:[{text:Hello}]}}]}"; // Invalid JSON
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
    public void Parse_WhenJsonIsValidButNoCandidates_ReturnsNull()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WhenDataHasTextButNoParts_ReturnsNull()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{""candidates"":[{""content"":{""parts"":[]}}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WhenDataHasTextWithEmptyString_ReturnsNull()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{""candidates"":[{""content"":{""parts"":[{""text"":""""}]}}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WhenDataHasFinishReasonButNoCandidates_ReturnsNull()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{""finishReason"":""STOP""}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WhenDataContainsExtraFields_IgnoresThem()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{""extra"":""value"",""candidates"":[{""content"":{""parts"":[{""text"":""Hello""}]}}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
    }
}