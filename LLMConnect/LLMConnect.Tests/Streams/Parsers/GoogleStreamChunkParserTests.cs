using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;

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

    // ---------- Text deltas ----------
    [Fact]
    public void Parse_WithTextDelta_ReturnsTextChunk()
    {
        var json = @"{""candidates"":[{""content"":{""parts"":[{""text"":""Hello""}]}}]}";
        var evt = new StreamEvent(null, json);
        var parser = new GoogleStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        result.IsComplete.Should().BeFalse();
        result.ToolCalls.Should().BeNull();
    }

    [Fact]
    public void Parse_WithMultipleTextParts_ConcatenatesText()
    {
        var json = @"{""candidates"":[{""content"":{""parts"":[
            {""text"":""Hello""},
            {""text"":"" world""}
        ]}}]}";
        var evt = new StreamEvent(null, json);
        var parser = new GoogleStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello world");
        result.IsComplete.Should().BeFalse();
    }

    // ---------- Tool call deltas ----------
    [Fact]
    public void Parse_WithFunctionCallDelta_ReturnsToolCallChunk()
    {
        var json = @"{""candidates"":[{""content"":{""parts"":[{""functionCall"":{""name"":""get_weather"",""args"":{""location"":""Boston""}}}]}}]}";
        var evt = new StreamEvent(null, json);
        var parser = new GoogleStreamChunkParser(_options);

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
    public void Parse_WithMultipleFunctionCalls_ReturnsAllToolCalls()
    {
        var json = @"{""candidates"":[{""content"":{""parts"":[
            {""functionCall"":{""name"":""get_weather"",""args"":{""location"":""Boston""}}},
            {""functionCall"":{""name"":""get_time"",""args"":{""timezone"":""EST""}}}
        ]}}]}";
        var evt = new StreamEvent(null, json);
        var parser = new GoogleStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.ToolCalls.Should().HaveCount(2);
        result.ToolCalls[0].Name.Should().Be("get_weather");
        result.ToolCalls[0].ArgumentsDelta.Should().Be("{\"location\":\"Boston\"}");
        result.ToolCalls[1].Name.Should().Be("get_time");
        result.ToolCalls[1].ArgumentsDelta.Should().Be("{\"timezone\":\"EST\"}");
        result.Content.Should().BeNull();
        result.IsComplete.Should().BeFalse();
    }

    // ---------- Finish reason ----------
    [Fact]
    public void Parse_WithFinishReason_ReturnsCompleteChunk()
    {
        var json = @"{""candidates"":[{""finishReason"":""STOP""}]}";
        var evt = new StreamEvent(null, json);
        var parser = new GoogleStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.IsComplete.Should().BeTrue();
        result.FinishReason.Should().Be("STOP");
        result.Content.Should().BeNull();
        result.ToolCalls.Should().BeNull();
    }

    [Fact]
    public void Parse_WithFinishReasonAndText_ReturnsCompleteChunkWithContent()
    {
        var json = @"{""candidates"":[{""content"":{""parts"":[{""text"":""Final""}]},""finishReason"":""STOP""}]}";
        var evt = new StreamEvent(null, json);
        var parser = new GoogleStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.Content.Should().Be("Final");
        result.IsComplete.Should().BeTrue();
        result.FinishReason.Should().Be("STOP");
    }

    // ---------- Edge cases ----------
    [Fact]
    public void Parse_WithNoCandidate_ReturnsNull()
    {
        var json = @"{}";
        var evt = new StreamEvent(null, json);
        var parser = new GoogleStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithEmptyParts_ReturnsEmptyChunk()
    {
        var json = @"{""candidates"":[{""content"":{""parts"":[]}}]}";
        var evt = new StreamEvent(null, json);
        var parser = new GoogleStreamChunkParser(_options);

        var result = parser.Parse(evt);

        result.Should().NotBeNull();
        result.Content.Should().BeNull();
        result.ToolCalls.Should().BeNull();
        result.IsComplete.Should().BeFalse();
        result.FinishReason.Should().BeNull();
    }

    [Fact]
    public void Parse_WithMalformedJson_LogsAndReturnsNull()
    {
        var malformed = "{ incomplete json";
        var evt = new StreamEvent(null, malformed);
        var parser = new GoogleStreamChunkParser(_options);

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