using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;

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

    // ---------- Text deltas ----------
    [Fact]
    public void Parse_WithTextDelta_ReturnsTextChunk()
    {
        var json = @"{""type"":""content_block_delta"",""index"":0,""delta"":{""type"":""text_delta"",""text"":""Hello""}}";
        var evt = new StreamEvent("content_block_delta", json);
        var parser = new AnthropicStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        result.IsComplete.Should().BeFalse();
        result.ToolCalls.Should().BeNull();
    }

    // ---------- Tool call deltas (single) ----------
    [Fact]
    public void Parse_WithToolUseStartAndDelta_ReturnsToolCallChunkWithPartialArguments()
    {
        var startJson = @"{""type"":""content_block_start"",""index"":0,""content_block"":{""type"":""tool_use"",""id"":""toolu_123"",""name"":""get_weather""}}";
        var startEvt = new StreamEvent("content_block_start", startJson);
        var parser = new AnthropicStreamChunkParser(_options);
        parser.Parse(startEvt); // sets up state

        var deltaJson = @"{""type"":""content_block_delta"",""index"":0,""delta"":{""type"":""input_json_delta"",""partial_json"":""{\""location\"":\""Boston\""}""}}";
        var deltaEvt = new StreamEvent("content_block_delta", deltaJson);

        var result = parser.Parse(deltaEvt);

        result.Should().NotBeNull();
        result.ToolCalls.Should().HaveCount(1);
        var tc = result.ToolCalls[0];
        tc.Index.Should().Be(0);
        tc.Id.Should().Be("toolu_123");
        tc.Name.Should().Be("get_weather");
        tc.ArgumentsDelta.Should().Be("{\"location\":\"Boston\"}");
        result.Content.Should().BeNull();
        result.IsComplete.Should().BeFalse();
    }

    // ---------- Multiple deltas (accumulation) ----------

    [Fact]
    public void Parse_WithMultipleToolUseDeltas_AccumulatesArgumentsCorrectly()
    {
        // Arrange
        var startJson = """
        {
            "type": "content_block_start",
            "index": 0,
            "content_block": {
                "type": "tool_use",
                "id": "toolu_123",
                "name": "get_weather"
            }
        }
        """;
        var startEvt = new StreamEvent("content_block_start", startJson);
        var parser = new AnthropicStreamChunkParser(_options);
        parser.Parse(startEvt); // sets up state

        // First delta: partial JSON fragment
        var deltaJson1 = """
        {
            "type": "content_block_delta",
            "index": 0,
            "delta": {
                "type": "input_json_delta",
                "partial_json": "{\"location\":\""
            }
        }
        """;
        var deltaEvt1 = new StreamEvent("content_block_delta", deltaJson1);
        var result1 = parser.Parse(deltaEvt1);

        // Assert first delta
        result1.Should().NotBeNull();
        result1.ToolCalls.Should().HaveCount(1);
        result1.ToolCalls[0].ArgumentsDelta.Should().Be("{\"location\":\"");

        // Second delta: rest of the JSON
        var deltaJson2 = """
        {
            "type": "content_block_delta",
            "index": 0,
            "delta": {
                "type": "input_json_delta",
                "partial_json": "Boston\""
            }
        }
        """;
        var deltaEvt2 = new StreamEvent("content_block_delta", deltaJson2);
        var result2 = parser.Parse(deltaEvt2);

        // Assert second delta
        result2.Should().NotBeNull();
        result2.ToolCalls.Should().HaveCount(1);
        result2.ToolCalls[0].ArgumentsDelta.Should().Be("Boston\"");
    }

    // ---------- Multiple indices ----------
    [Fact]
    public void Parse_WithMultipleToolUseIndices_HandlesSeparateStates()
    {
        // Tool 1
        var parser = new AnthropicStreamChunkParser(_options);

        var start1 = @"{""type"":""content_block_start"",""index"":0,""content_block"":{""type"":""tool_use"",""id"":""toolu_1"",""name"":""foo""}}";
        parser.Parse(new StreamEvent("content_block_start", start1));

        var delta1 = @"{""type"":""content_block_delta"",""index"":0,""delta"":{""type"":""input_json_delta"",""partial_json"":""{\""a\"":1}""}}";
        var result1 = parser.Parse(new StreamEvent("content_block_delta", delta1));
        result1.ToolCalls[0].Id.Should().Be("toolu_1");

        // Tool 2
        var start2 = @"{""type"":""content_block_start"",""index"":1,""content_block"":{""type"":""tool_use"",""id"":""toolu_2"",""name"":""bar""}}";
        parser.Parse(new StreamEvent("content_block_start", start2));

        var delta2 = @"{""type"":""content_block_delta"",""index"":1,""delta"":{""type"":""input_json_delta"",""partial_json"":""{\""b\"":2}""}}";
        var result2 = parser.Parse(new StreamEvent("content_block_delta", delta2));
        result2.ToolCalls[0].Id.Should().Be("toolu_2");
        result2.ToolCalls[0].Name.Should().Be("bar");
    }

    // ---------- Finish reason ----------
    [Fact]
    public void Parse_WithMessageDelta_ReturnsCompleteChunkWithFinishReason()
    {
        var json = @"{""type"":""message_delta"",""delta"":{""stop_reason"":""end_turn""},""usage"":{""output_tokens"":5}}";
        var evt = new StreamEvent("message_delta", json);
        var parser = new AnthropicStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.IsComplete.Should().BeTrue();
        result.FinishReason.Should().Be("end_turn");
        result.Content.Should().BeNull();
        result.ToolCalls.Should().BeNull();
    }

    [Fact]
    public void Parse_WithMessageDeltaWithoutStopReason_ReturnsNull()
    {
        var json = @"{""type"":""message_delta"",""delta"":{},""usage"":{}}";
        var evt = new StreamEvent("message_delta", json);
        var parser = new AnthropicStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().BeNull();
    }

    // ---------- Message stop ----------
    [Fact]
    public void Parse_WithMessageStop_ReturnsCompleteChunk()
    {
        var evt = new StreamEvent("message_stop", "{}");
        var parser = new AnthropicStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.IsComplete.Should().BeTrue();
        result.FinishReason.Should().BeNull();
        result.Content.Should().BeNull();
        result.ToolCalls.Should().BeNull();
    }

    // ---------- Edge cases ----------
    [Fact]
    public void Parse_WithContentBlockStartNotToolUse_ReturnsNull()
    {
        var json = @"{""type"":""content_block_start"",""index"":0,""content_block"":{""type"":""text"",""text"":""hi""}}";
        var evt = new StreamEvent("content_block_start", json);
        var parser = new AnthropicStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithMalformedJson_LogsAndReturnsNull()
    {
        var malformed = "{ incomplete json";
        var evt = new StreamEvent("content_block_delta", malformed);
        var parser = new AnthropicStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().BeNull();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ignoring malformed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Parse_WithUnknownEvent_ReturnsNull()
    {
        var evt = new StreamEvent("ping", "{}");
        var parser = new AnthropicStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().BeNull();
    }
}