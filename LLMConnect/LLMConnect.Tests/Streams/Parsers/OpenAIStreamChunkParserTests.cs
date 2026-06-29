using FluentAssertions;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMConnect.Tests.Streams.ChunkParsers;

public class OpenAIStreamChunkParserTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly LLMConnectClientOptions _options;

    public OpenAIStreamChunkParserTests()
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
    public void Parse_WhenDataHasDeltaContent_ReturnsChatChunk()
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
    }

    [Fact]
    public void Parse_WhenDataHasDeltaContentWithSpecialCharacters_ReturnsCorrectText()
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
    public void Parse_WhenDataHasMultipleChoices_UsesFirst()
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
    public void Parse_WhenDataIsDoneSentinel_ReturnsNull()
    {
        // Arrange
        var parser = new OpenAIStreamChunkParser(_options);
        var evt = new StreamEvent(null, "[DONE]");

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
        var parser = new OpenAIStreamChunkParser(_options);

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
        var parser = new OpenAIStreamChunkParser(_options);
        var malformedJson = "{choices:[{delta:{content:Hello}}]}"; // Invalid JSON
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
    public void Parse_WhenDataHasNoChoices_ReturnsNull()
    {
        // Arrange
        var parser = new OpenAIStreamChunkParser(_options);
        var json = @"{}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WhenDataHasChoicesButNoDelta_ReturnsNull()
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
    public void Parse_WhenDataHasDeltaButContentIsNull_ReturnsNull()
    {
        // Arrange
        var parser = new OpenAIStreamChunkParser(_options);
        var json = @"{""choices"":[{""delta"":{""content"":null}}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WhenDataHasDeltaButContentIsEmpty_ReturnsNull()
    {
        // Arrange
        var parser = new OpenAIStreamChunkParser(_options);
        var json = @"{""choices"":[{""delta"":{""content"":""""}}]}";
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
        var parser = new OpenAIStreamChunkParser(_options);
        var json = @"{""extra"":""value"",""choices"":[{""delta"":{""content"":""Hello""}}]}";
        var evt = new StreamEvent(null, json);

        // Act
        var result = parser.Parse(evt);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
    }
}