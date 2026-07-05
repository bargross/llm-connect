using FluentAssertions;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace LLMConnect.Tests.Streams.ChunkParsers;

public class GoogleStreamChunkParserTests
{
    private readonly Mock<ILogger<GoogleStreamChunkParser>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectGeneralOptions _options;

    public GoogleStreamChunkParserTests()
    {
        _loggerMock = new Mock<ILogger<GoogleStreamChunkParser>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _options = new LLMConnectGeneralOptions
        {
            LoggerFactory = _loggerFactoryMock.Object
        };
    }

    // ---------- Text Chunks ----------

    [Fact]
    public void Parse_WithTextChunk_ReturnsIncompleteChatChunk()
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
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WithTextChunkAndSpecialCharacters_ReturnsCorrectText()
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
    public void Parse_WithTextChunkAndExtraFields_IgnoresExtraFields()
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

    // ---------- Finish Reason Chunks ----------

    [Fact]
    public void Parse_WithFinishReasonChunk_ReturnsCompleteChatChunk()
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
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WithFinishReasonMaxTokens_ReturnsCompleteChunkWithReason()
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

    // ---------- Chunks with Both Text and Finish Reason ----------

    [Fact]
    public void Parse_WithTextAndFinishReason_PrioritizesText()
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
        result.IsComplete.Should().BeFalse(); // Text takes precedence
        result.FinishReason.Should().BeNull();
    }

    // ---------- Empty or Null Data ----------

    [Fact]
    public void Parse_WithNullData_ReturnsNull()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
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
        var parser = new GoogleStreamChunkParser(_options);
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
        var parser = new GoogleStreamChunkParser(_options);
        var malformedJson = "{candidates:[{content:{parts:[{text:Hello}]}}]}";
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

    // ---------- JSON with Missing Fields ----------

    [Fact]
    public void Parse_WithNoCandidates_ReturnsNull()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WithEmptyCandidates_ReturnsNull()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{""candidates"":[]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WithNoContentInCandidate_ReturnsNull()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{""candidates"":[{""finishReason"":""STOP""}]}"; // This has finishReason, so it should return a complete chunk.
        // Actually, this case is already covered. We need a case with no content and no finishReason.
        // Let's test candidate without content and without finishReason.
        var json2 = @"{""candidates"":[{}]}";
        var evt = new StreamEvent(null, json2);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithContentButNoParts_ReturnsNull()
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
    public void Parse_WithPartsButNoText_ReturnsNull()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{""candidates"":[{""content"":{""parts"":[{""extra"":""value""}]}}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithTextButEmptyString_ReturnsNull()
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

    // ---------- Usage Metadata Only ----------

    [Fact]
    public void Parse_WithUsageMetadataOnly_ReturnsNull()
    {
        // Arrange
        var parser = new GoogleStreamChunkParser(_options);
        var json = @"{""usageMetadata"":{""promptTokenCount"":10}}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyNoOtherCalls();
    }
}