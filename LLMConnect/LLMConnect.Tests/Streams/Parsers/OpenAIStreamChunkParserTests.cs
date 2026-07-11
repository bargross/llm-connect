using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;

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

    // ---------- Text deltas ----------
    [Fact]
    public void Parse_WithTextDelta_ReturnsTextChunk()
    {
        var json = @"{""choices"":[{""delta"":{""content"":""Hello""}}]}";
        var evt = new StreamEvent(null, json);
        var parser = new OpenAIStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        result.IsComplete.Should().BeFalse();
        result.ToolCalls.Should().BeNull();
    }

    // ---------- Tool call deltas ----------
    [Fact]
    public void Parse_WithSingleToolCallDelta_ReturnsToolCallChunk()
    {
        var json = @"{""choices"":[{""delta"":{""tool_calls"":[{""index"":0,""id"":""call_123"",""type"":""function"",""function"":{""name"":""get_weather"",""arguments"":""{\""location\"":\""Boston\""}""}}]}}]}";
        var evt = new StreamEvent(null, json);
        var parser = new OpenAIStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.ToolCalls.Should().HaveCount(1);
        var tc = result.ToolCalls[0];
        tc.Index.Should().Be(0);
        tc.Id.Should().Be("call_123");
        tc.Name.Should().Be("get_weather");
        tc.ArgumentsDelta.Should().Be("{\"location\":\"Boston\"}");
        result.Content.Should().BeNull();
        result.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void Parse_WithMultipleToolCallsInOneDelta_ReturnsAllToolCalls()
    {
        var json = @"{""choices"":[{""delta"":{""tool_calls"":[
            {""index"":0,""id"":""call_123"",""function"":{""name"":""get_weather"",""arguments"":""{\""location\"":\""Boston\""}""}},
            {""index"":1,""id"":""call_456"",""function"":{""name"":""get_time"",""arguments"":""{\""timezone\"":\""EST\""}""}}
        ]}}]}";
        var evt = new StreamEvent(null, json);
        var parser = new OpenAIStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.ToolCalls.Should().HaveCount(2);
        result.ToolCalls[0].Name.Should().Be("get_weather");
        result.ToolCalls[1].Name.Should().Be("get_time");
        result.IsComplete.Should().BeFalse();
    }

    // ---------- Combined content and tool calls ----------
    [Fact]
    public void Parse_WithTextAndToolCalls_IncludesBoth()
    {
        var json = @"{""choices"":[{""delta"":{""content"":""Weather: "",""tool_calls"":[{""index"":0,""id"":""call_123"",""function"":{""name"":""get_weather"",""arguments"":""{\""location\"":\""Boston\""}""}}]}}]}";
        var evt = new StreamEvent(null, json);
        var parser = new OpenAIStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.Content.Should().Be("Weather: ");
        result.ToolCalls.Should().HaveCount(1);
        result.ToolCalls[0].ArgumentsDelta.Should().Be("{\"location\":\"Boston\"}");
        result.IsComplete.Should().BeFalse();
    }

    // ---------- Finish reason ----------
    [Fact]
    public void Parse_WithFinishReason_ReturnsCompleteChunk()
    {
        var json = @"{""choices"":[{""finish_reason"":""stop""}]}";
        var evt = new StreamEvent(null, json);
        var parser = new OpenAIStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.IsComplete.Should().BeTrue();
        result.FinishReason.Should().Be("stop");
        result.Content.Should().BeNull();
        result.ToolCalls.Should().BeNull();
    }

    [Fact]
    public void Parse_WithFinishReasonAndContent_ReturnsCompleteChunkWithContent()
    {
        var json = @"{""choices"":[{""delta"":{""content"":""Final""},""finish_reason"":""stop""}]}";
        var evt = new StreamEvent(null, json);
        var parser = new OpenAIStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.Content.Should().Be("Final");
        result.IsComplete.Should().BeTrue();
        result.FinishReason.Should().Be("stop");
    }

    // ---------- Edge cases ----------
    [Fact]
    public void Parse_WithDONE_ReturnsNull()
    {
        var evt = new StreamEvent(null, "[DONE]");
        var parser = new OpenAIStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithEmptyDelta_ReturnsEmptyChunkWithIsCompleteFalse()
    {
        var json = @"{""choices"":[{""delta"":{}}]}";
        var evt = new StreamEvent(null, json);
        var parser = new OpenAIStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.Content.Should().BeNull();
        result.ToolCalls.Should().BeNull();
        result.IsComplete.Should().BeFalse();
        result.FinishReason.Should().BeNull();
    }

    [Fact]
    public void Parse_WithNoChoices_ReturnsNull()
    {
        var json = @"{}";
        var evt = new StreamEvent(null, json);
        var parser = new OpenAIStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithMalformedJson_LogsAndReturnsNull()
    {
        var malformed = "{ incomplete json";
        var evt = new StreamEvent(null, malformed);
        var parser = new OpenAIStreamChunkParser(_options);

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
}