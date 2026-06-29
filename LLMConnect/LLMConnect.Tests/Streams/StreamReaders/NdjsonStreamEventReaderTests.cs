using FluentAssertions;
using LLMConnect.Settings;
using LLMConnect.Streams.StreamReaders;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace LLMConnect.Tests.Streams.StreamReaders;

public class NdjsonStreamEventReaderTests
{
    [Fact]
    public async Task ReadEventsAsync_WithDataPrefix_YieldsCorrectEvents()
    {
        // Arrange
        var ndjsonData = @"
            data: {""id"":""1"",""content"":""Hello""}
            data: {""id"":""2"",""content"":"" world""}
        ".Trim();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjsonData));
        var reader = new NdjsonStreamEventReader();

        // Act
        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        // Assert
        events.Should().HaveCount(2);
        events[0].EventName.Should().BeNull();
        events[0].Data.Should().Be(@"{""id"":""1"",""content"":""Hello""}");
        events[1].EventName.Should().BeNull();
        events[1].Data.Should().Be(@"{""id"":""2"",""content"":"" world""}");
    }

    [Fact]
    public async Task ReadEventsAsync_WithRawJsonLines_YieldsCorrectEvents()
    {
        // Arrange
        var ndjsonData = @"
            {""id"":""1"",""content"":""Hello""}
            {""id"":""2"",""content"":"" world""}
        ".Trim();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjsonData));
        var reader = new NdjsonStreamEventReader();

        // Act
        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        // Assert
        events.Should().HaveCount(2);
        events[0].EventName.Should().BeNull();
        events[0].Data.Should().Be(@"{""id"":""1"",""content"":""Hello""}");
        events[1].EventName.Should().BeNull();
        events[1].Data.Should().Be(@"{""id"":""2"",""content"":"" world""}");
    }

    [Fact]
    public async Task ReadEventsAsync_WithMixedDataPrefixAndRawLines_HandlesBoth()
    {
        // Arrange
        var ndjsonData = @"
            data: {""type"":""openai""}
            {""type"":""ollama""}
            data: {""type"":""openai2""}
        ".Trim();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjsonData));
        var reader = new NdjsonStreamEventReader();

        // Act
        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        // Assert
        events.Should().HaveCount(3);
        events[0].Data.Should().Be(@"{""type"":""openai""}");
        events[1].Data.Should().Be(@"{""type"":""ollama""}");
        events[2].Data.Should().Be(@"{""type"":""openai2""}");
    }

    [Fact]
    public async Task ReadEventsAsync_WithDoneSentinel_YieldsBreak()
    {
        // Arrange
        var ndjsonData = @"
            data: {""id"":""1""}
            data: [DONE]
            data: {""id"":""2""}
        ".Trim();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjsonData));
        var reader = new NdjsonStreamEventReader();

        // Act
        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        // Assert
        events.Should().HaveCount(2); // Only the first data and the sentinel event
        events[0].Data.Should().Be(@"{""id"":""1""}");
        events[1].Data.Should().Be("[DONE]");
        // The third line should not be read because the sentinel caused yield break
    }

    [Fact]
    public async Task ReadEventsAsync_WithEmptyLines_SkipsThem()
    {
        // Arrange
        var ndjsonData = @"
            data: {""id"":""1""}
            {""id"":""2""}
        ".TrimStart(); // Keep leading newline
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjsonData));
        var reader = new NdjsonStreamEventReader();

        // Act
        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        // Assert
        events.Should().HaveCount(2);
        events[0].Data.Should().Be(@"{""id"":""1""}");
        events[1].Data.Should().Be(@"{""id"":""2""}");
    }

    [Fact]
    public async Task ReadEventsAsync_WithCancellation_LogsAndBreaks()
    {
        // Arrange
        var loggerMock = new Mock<ILogger>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(loggerMock.Object);

        var options = new LLMConnectClientOptions
        {
            LoggerFactory = loggerFactoryMock.Object
        };
        var reader = new NdjsonStreamEventReader(options);

        var ndjsonData = @"
            data: hello
        ".Trim();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjsonData));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var enumerator = reader.ReadEventsAsync(stream, cts.Token).GetAsyncEnumerator();
        var result = await enumerator.MoveNextAsync();

        // Assert
        result.Should().BeFalse();

        loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAI stream has ended.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReadEventsAsync_WithNoData_ReturnsEmpty()
    {
        // Arrange
        var ndjsonData = "";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjsonData));
        var reader = new NdjsonStreamEventReader();

        // Act
        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadEventsAsync_WithLogger_LogsOnCancellation()
    {
        // Arrange
        var loggerMock = new Mock<ILogger>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(loggerMock.Object);

        var options = new LLMConnectClientOptions
        {
            LoggerFactory = loggerFactoryMock.Object
        };
        var reader = new NdjsonStreamEventReader(options);

        var ndjsonData = @"
            data: hello
        ".Trim();
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjsonData));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var enumerator = reader.ReadEventsAsync(stream, cts.Token).GetAsyncEnumerator();
        await enumerator.MoveNextAsync();

        // Assert
        loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAI stream has ended.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}