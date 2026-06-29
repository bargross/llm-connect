using FluentAssertions;
using LLMConnect.Settings;
using LLMConnect.Streams.StreamReaders;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace LLMConnect.Tests.Streams.StreamReaders;

public class SseStreamEventReaderTests
{
    [Fact]
    public async Task ReadEventsAsync_WithEventAndData_YieldsCorrectEvents()
    {
        // Arrange
        var sseData = @"
            event: content_block_delta
            data: {""delta"":{""text"":""Hello""}}

            event: content_block_delta
            data: {""delta"":{""text"":"" world""}}
        ".Trim();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));
        var reader = new SseStreamEventReader();

        // Act
        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        // Assert
        events.Should().HaveCount(2);
        events[0].EventName.Should().Be("content_block_delta");
        events[0].Data.Should().Be(@"{""delta"":{""text"":""Hello""}}");
        events[1].EventName.Should().Be("content_block_delta");
        events[1].Data.Should().Be(@"{""delta"":{""text"":"" world""}}");
    }

    [Fact]
    public async Task ReadEventsAsync_WithDataOnly_EventNameIsNull()
    {
        // Arrange
        var sseData = @"
            data: {""choices"":[{""delta"":{""content"":""Hello""}}]}
        ".Trim();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));
        var reader = new SseStreamEventReader();

        // Act
        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        // Assert
        events.Should().HaveCount(1);
        events[0].EventName.Should().BeNull();
        events[0].Data.Should().Be(@"{""choices"":[{""delta"":{""content"":""Hello""}}]}");
    }

    [Fact]
    public async Task ReadEventsAsync_WithMultipleEvents_YieldsAll()
    {
        // Arrange
        var sseData = @"
            event: message_start
            data: {""id"":""msg_123""}

            event: content_block_delta
            data: {""delta"":{""text"":""Hello""}}

            event: message_stop
            data: {}
        ".Trim();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));
        var reader = new SseStreamEventReader();

        // Act
        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        // Assert
        events.Should().HaveCount(3);
        events[0].EventName.Should().Be("message_start");
        events[0].Data.Should().Be(@"{""id"":""msg_123""}");
        events[1].EventName.Should().Be("content_block_delta");
        events[1].Data.Should().Be(@"{""delta"":{""text"":""Hello""}}");
        events[2].EventName.Should().Be("message_stop");
        events[2].Data.Should().Be("{}");
    }

    [Fact]
    public async Task ReadEventsAsync_WithDoneSentinel_YieldsBreak()
    {
        // Arrange
        var sseData = @"
            event: message_start
            data: {""id"":""msg_123""}

            data: [DONE]
        ".Trim();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));
        var reader = new SseStreamEventReader();

        // Act
        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        // Assert
        events.Should().HaveCount(2);
        events[0].EventName.Should().Be("message_start");
        events[0].Data.Should().Be(@"{""id"":""msg_123""}");
        events[1].EventName.Should().Be("message_start"); // event name is carried over? Actually after the break it yields the sentinel event.
        events[1].Data.Should().Be("[DONE]");
    }

    [Fact]
    public async Task ReadEventsAsync_WithCancellation_LogsAndBreaks()
    {
        // Arrange
        var loggerMock = new Mock<ILogger>();
        var options = new LLMConnectClientOptions
        {
            LoggerFactory = LoggerFactory.Create(builder => builder.AddConsole())
        };

        // Use a custom logger factory that returns our mock
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(loggerMock.Object);
        options.LoggerFactory = loggerFactoryMock.Object;

        var reader = new SseStreamEventReader(options);

        // We need a stream that will cause a cancellation during ReadLineAsync.
        // The simplest is to use a stream that never ends and then cancel after a short delay.
        // Or we can use a CancellationTokenSource and cancel after some time.
        // We'll simulate by passing a cancellation token that is already cancelled.
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Use a memory stream with valid SSE data, but cancellation will happen before any read.
        var sseData = @"
            data: hello
        ".Trim();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

        // Act: ReadEventsAsync will be called with the cancelled token.
        // It will hit the read line and throw OperationCanceledException.
        var enumerator = reader.ReadEventsAsync(stream, cts.Token).GetAsyncEnumerator();
        var moveNextTask = enumerator.MoveNextAsync();

        // Since cancellation is immediate, we should get an OperationCanceledException (or the loop breaks).
        // However, the code catches OperationCanceledException and breaks, so moveNextTask should return false (completed normally).
        // Actually it breaks and returns false. We need to check that it completed and logged.
        var result = await moveNextTask; // Should be false
        result.Should().BeFalse();

        // Verify logger logged the error message.
        loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stream has ended.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReadEventsAsync_WithEmptyLines_SkipsThem()
    {
        // Arrange
        var sseData = @"
            event: test
            data: 123
        ".TrimStart(); // Keep leading newline

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));
        var reader = new SseStreamEventReader();

        // Act
        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        // Assert
        events.Should().HaveCount(1);
        events[0].EventName.Should().Be("test");
        events[0].Data.Should().Be("123");
    }

    [Fact]
    public async Task ReadEventsAsync_WithInvalidLines_Ignores()
    {
        // Arrange
        var sseData = @"
            invalid: line
            event: test
            data: 123
        ".Trim();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));
        var reader = new SseStreamEventReader();

        // Act
        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        // Assert
        events.Should().HaveCount(1);
        events[0].EventName.Should().Be("test");
        events[0].Data.Should().Be("123");
    }

    [Fact]
    public async Task ReadEventsAsync_WithNoData_ReturnsEmpty()
    {
        // Arrange
        var sseData = "";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));
        var reader = new SseStreamEventReader();

        // Act
        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        // Assert
        events.Should().BeEmpty();
    }
}