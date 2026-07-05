using FluentAssertions;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace LLMConnect.Tests.Streams.ChunkParsers;

public class OpenAIStreamChunkParserTests
{
    private readonly Mock<ILogger<OpenAIStreamChunkParser>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectGeneralOptions _options;

    public OpenAIStreamChunkParserTests()
    {
        _loggerMock = new Mock<ILogger<OpenAIStreamChunkParser>>();
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
    public void Parse_WithValidContent_ReturnsChatChunk()
    {
        // Arrange
        var parser = new OpenAIStreamChunkParser(_options);
        var json = @"{""choices"":[{""delta"":{""content"":""Hello""}}]}";
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
    public void Parse_WithContentAndSpecialCharacters_ReturnsCorrectText()
    {
        // Arrange
        var parser = new OpenAIStreamChunkParser(_options);
        var json = @"{""choices"":[{""delta"":{""content"":""Hello\nworld""}}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello\nworld");
    }

    [Fact]
    public void Parse_WithMultipleChoices_UsesFirst()
    {
        // Arrange
        var parser = new OpenAIStreamChunkParser(_options);
        var json = @"{""choices"":[{""delta"":{""content"":""Hello""}},{""delta"":{""content"":""World""}}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
    }

    [Fact]
    public void Parse_WithExtraFields_IgnoresThem()
    {
        // Arrange
        var parser = new OpenAIStreamChunkParser(_options);
        var json = @"{""extra"":""value"",""choices"":[{""delta"":{""content"":""Hello""}}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
    }

    // ---------- Empty Content ----------

    [Fact]
    public void Parse_WithEmptyContent_ReturnsNull()
    {
        // Arrange
        var parser = new OpenAIStreamChunkParser(_options);
        var json = @"{""choices"":[{""delta"":{""content"":""""}}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WithNullContent_ReturnsNull()
    {
        // Arrange
        var parser = new OpenAIStreamChunkParser(_options);
        var json = @"{""choices"":[{""delta"":{""content"":null}}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Missing Fields ----------

    [Fact]
    public void Parse_WithNoChoices_ReturnsNull()
    {
        // Arrange
        var parser = new OpenAIStreamChunkParser(_options);
        var json = @"{}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Parse_WithChoicesButNoDelta_ReturnsNull()
    {
        // Arrange
        var parser = new OpenAIStreamChunkParser(_options);
        var json = @"{""choices"":[{""finish_reason"":""stop""}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithDeltaButNoContent_ReturnsNull()
    {
        // Arrange
        var parser = new OpenAIStreamChunkParser(_options);
        var json = @"{""choices"":[{""delta"":{}}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
    }

    // ---------- Done Sentinel ----------

    [Fact]
    public void Parse_WithDoneSentinel_ReturnsNull()
    {
        // Arrange
        var parser = new OpenAIStreamChunkParser(_options);
        var evt = new StreamEvent(null, "[DONE]");

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
        var parser = new OpenAIStreamChunkParser(_options);
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
        var parser = new OpenAIStreamChunkParser(_options);
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
        var parser = new OpenAIStreamChunkParser(_options);
        var malformedJson = "{choices:[{delta:{content:Hello}}]}";
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
}