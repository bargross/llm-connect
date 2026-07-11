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
    public void Parse_WithUnknownEvent_ReturnsNull()
    {
        var evt = new StreamEvent("ping", "{}");
        var parser = new AnthropicStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().BeNull();
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

    [Fact]
    public void Parse_WithMessageStopEvent_ReturnsCompleteChunk()
    {
        var evt = new StreamEvent("message_stop", "{}");
        var parser = new AnthropicStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.IsComplete.Should().BeTrue();
        result.Content.Should().BeNull();
        result.ToolCalls.Should().BeNull();
    }

    // ---------- Malformed JSON ----------

    [Fact]
    public void Parse_WithMalformedJson_LogsAndReturnsNull()
    {
        // Arrange
        var malformed = "{ incomplete json";
        var evt = new StreamEvent("content_block_delta", malformed);
        var parser = new AnthropicStreamChunkParser(_options);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();

        // Verify log – use partial match because the actual message varies
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ignoring malformed")),
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

    [Fact]
    public void Parse_WithToolUseDelta_ReturnsToolCallChunk()
    {
        var startJson = @"{""type"":""content_block_start"",""index"":0,""content_block"":{""type"":""tool_use"",""id"":""toolu_123"",""name"":""get_weather""}}";
        var startEvt = new StreamEvent("content_block_start", startJson);
        var options = new LLMConnectGeneralOptions();
        var parser = new AnthropicStreamChunkParser(options);
        parser.Parse(startEvt); // ignore start

        var deltaJson = @"{""type"":""content_block_delta"",""index"":0,""delta"":{""type"":""tool_use_delta"",""partial_json"":""{\""location\"":\""Boston\""}""}}";
        var deltaEvt = new StreamEvent("content_block_delta", deltaJson);

        var result = parser.Parse(deltaEvt);

        result.Should().NotBeNull();
        result.ToolCalls.Should().HaveCount(1);
        var tc = result.ToolCalls[0];
        tc.Index.Should().Be(0);
        tc.Id.Should().Be("toolu_123");
        tc.Name.Should().Be("get_weather");
        tc.ArgumentsDelta.Should().Be("{\"location\":\"Boston\"}"); // ✅ Use ArgumentsDelta
    }
}