using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;

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

    // ---------- Text deltas ----------
    [Fact]
    public void Parse_WithTextDelta_ReturnsTextChunk()
    {
        var ndjson = @"{""message"":{""content"":""Hello""},""done"":false}";
        var evt = new StreamEvent(null, ndjson);
        var parser = new OllamaStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        result.IsComplete.Should().BeFalse();
        result.ToolCalls.Should().BeNull();
    }

    // ---------- Tool calls ----------
    [Fact]
    public void Parse_WithToolCallsDelta_ReturnsToolCallChunk()
    {
        var ndjson = @"{""message"":{""role"":""assistant"",""content"":"""",""tool_calls"":[{""function"":{""name"":""get_weather"",""arguments"":{""location"":""Boston""}}}]},""done"":false}";
        var evt = new StreamEvent(null, ndjson);
        var parser = new OllamaStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.ToolCalls.Should().HaveCount(1);
        var tc = result.ToolCalls[0];
        tc.Index.Should().Be(0);
        tc.Id.Should().Be("get_weather");
        tc.Name.Should().Be("get_weather");
        tc.ArgumentsDelta.Should().Be("{\"location\":\"Boston\"}");
        result.Content.Should().BeNull();
        result.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void Parse_WithMultipleToolCalls_ReturnsAllToolCalls()
    {
        var ndjson = @"{""message"":{""role"":""assistant"",""tool_calls"":[
            {""function"":{""name"":""get_weather"",""arguments"":{""location"":""Boston""}}},
            {""function"":{""name"":""get_time"",""arguments"":{""timezone"":""EST""}}}
        ]},""done"":false}";
        var evt = new StreamEvent(null, ndjson);
        var parser = new OllamaStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.ToolCalls.Should().HaveCount(2);
        result.ToolCalls[0].Name.Should().Be("get_weather");
        result.ToolCalls[1].Name.Should().Be("get_time");
    }

    // ---------- Done flag ----------
    [Fact]
    public void Parse_WithDoneFlag_ReturnsCompleteChunk()
    {
        var ndjson = @"{""message"":{""content"":""Done""},""done"":true,""done_reason"":""stop""}";
        var evt = new StreamEvent(null, ndjson);
        var parser = new OllamaStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.IsComplete.Should().BeTrue();
        result.FinishReason.Should().Be("stop");
        result.Content.Should().Be("Done");
        result.ToolCalls.Should().BeNull();
    }

    [Fact]
    public void Parse_WithDoneFlagWithoutReason_ReturnsCompleteChunkWithDefaultReason()
    {
        var ndjson = @"{""message"":{""content"":""Done""},""done"":true}";
        var evt = new StreamEvent(null, ndjson);
        var parser = new OllamaStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.IsComplete.Should().BeTrue();
        result.FinishReason.Should().Be("stop");
        result.Content.Should().Be("Done");
    }

    // ---------- Edge cases ----------
    [Fact]
    public void Parse_WithEmptyData_ReturnsNull()
    {
        var evt = new StreamEvent(null, "");
        var parser = new OllamaStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithMalformedJson_LogsAndReturnsNull()
    {
        var malformed = "{ incomplete json";
        var evt = new StreamEvent(null, malformed);
        var parser = new OllamaStreamChunkParser(_options);

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
    public void Parse_WithNullMessage_ReturnsCompleteChunk()
    {
        var ndjson = @"{""done"":true}";
        var evt = new StreamEvent(null, ndjson);
        var parser = new OllamaStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.IsComplete.Should().BeTrue();
        result.FinishReason.Should().Be("stop");
        result.Content.Should().BeNull();
        result.ToolCalls.Should().BeNull();
    }
}