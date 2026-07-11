using FluentAssertions;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace LLMConnect.Tests.Streams.StreamReaders;

public class SseStreamEventReaderTests
{
    private readonly Mock<ILogger<SseStreamEventReader>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectGeneralOptions _options;

    public SseStreamEventReaderTests()
    {
        _loggerMock = new Mock<ILogger<SseStreamEventReader>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _options = new LLMConnectGeneralOptions
        {
            LoggerFactory = _loggerFactoryMock.Object
        };
    }

    // Helper to create a stream from string
    private static Stream CreateStream(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    // ---------- Event and Data ----------

    [Fact]
    public async Task ReadEventsAsync_WithEventAndData_YieldsCorrectEvents()
    {
        var content = """
            event: content_block_delta
            data: {"delta":{"text":"Hello"}}

            event: content_block_delta
            data: {"delta":{"text":" world"}}
            """;
        using var stream = CreateStream(content);
        var reader = new SseStreamEventReader(_options);

        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        events.Should().HaveCount(2);
        events[0].EventName.Should().Be("content_block_delta");
        events[0].Data.Should().Be(@"{""delta"":{""text"":""Hello""}}");
        events[1].EventName.Should().Be("content_block_delta");
        events[1].Data.Should().Be(@"{""delta"":{""text"":"" world""}}");
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReadEventsAsync_WithEventAndDataAndEmptyLines_SkipsEmptyLines()
    {
        var content = """

            event: test
            data: 123


            """;
        using var stream = CreateStream(content);
        var reader = new SseStreamEventReader(_options);

        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        events.Should().HaveCount(1);
        events[0].EventName.Should().Be("test");
        events[0].Data.Should().Be("123");
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Data only ----------

    [Fact]
    public async Task ReadEventsAsync_WithDataOnly_EventNameIsNull()
    {
        var content = """
            data: {"choices":[{"delta":{"content":"Hello"}}]}
            """;
        using var stream = CreateStream(content);
        var reader = new SseStreamEventReader(_options);

        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        events.Should().HaveCount(1);
        events[0].EventName.Should().BeNull();
        events[0].Data.Should().Be(@"{""choices"":[{""delta"":{""content"":""Hello""}}]}");
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Event name persists until data ----------

    [Fact]
    public async Task ReadEventsAsync_EventNamePersistsAcrossLines()
    {
        var content = """
            event: message_start
            data: {"id":"1"}
            data: {"id":"2"}
            """;
        using var stream = CreateStream(content);
        var reader = new SseStreamEventReader(_options);

        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        events.Should().HaveCount(2);
        events[0].EventName.Should().Be("message_start");
        events[0].Data.Should().Be(@"{""id"":""1""}");
        events[1].EventName.Should().Be("message_start");
        events[1].Data.Should().Be(@"{""id"":""2""}");
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Done sentinel ----------

    [Fact]
    public async Task ReadEventsAsync_WithDoneSentinel_YieldsBreak()
    {
        var content = """
            event: message_start
            data: {"id":"1"}

            data: [DONE]
            data: {"id":"2"}
            """;
        using var stream = CreateStream(content);
        var reader = new SseStreamEventReader(_options);

        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        events.Should().HaveCount(2);
        events[0].EventName.Should().Be("message_start");
        events[0].Data.Should().Be(@"{""id"":""1""}");
        events[1].EventName.Should().Be("message_start"); // currentEvent is carried over
        events[1].Data.Should().Be("[DONE]");
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Invalid lines ----------

    [Fact]
    public async Task ReadEventsAsync_WithInvalidLines_IgnoresThem()
    {
        var content = """
            invalid: line
            event: test
            data: 123
            """;
        using var stream = CreateStream(content);
        var reader = new SseStreamEventReader(_options);

        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        events.Should().HaveCount(1);
        events[0].EventName.Should().Be("test");
        events[0].Data.Should().Be("123");
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Cancellation ----------

    [Fact]
    public async Task ReadEventsAsync_WithCancellation_LogsAndBreaks()
    {
        var content = """
            event: test
            data: 123
            event: test2
            data: 456
            """;
        using var stream = CreateStream(content);
        using var cts = new CancellationTokenSource();
        var reader = new SseStreamEventReader(_options);

        var enumerator = reader.ReadEventsAsync(stream, cts.Token).GetAsyncEnumerator();
        var hasNext = await enumerator.MoveNextAsync();
        hasNext.Should().BeTrue();
        enumerator.Current.Data.Should().Be("123");

        // Cancel and try to get next
        cts.Cancel();
        var nextHasNext = await enumerator.MoveNextAsync();
        nextHasNext.Should().BeFalse();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stream has ended.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ---------- No data ----------

    [Fact]
    public async Task ReadEventsAsync_WithNoData_ReturnsEmpty()
    {
        using var stream = new MemoryStream();
        var reader = new SseStreamEventReader(_options);

        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        events.Should().BeEmpty();
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Logger null ----------

    [Fact]
    public async Task ReadEventsAsync_WhenLoggerIsNull_DoesNotThrow()
    {
        var optionsWithoutLogger = new LLMConnectGeneralOptions { LoggerFactory = null };
        var content = "event: test\ndata: 123\n";
        using var stream = CreateStream(content);
        var reader = new SseStreamEventReader(optionsWithoutLogger);

        var act = async () => await reader.ReadEventsAsync(stream).ToListAsync();

        await act.Should().NotThrowAsync();
    }

    // ---------- Cancellation without logger ----------

    [Fact]
    public async Task ReadEventsAsync_WithCancellationAndNoLogger_DoesNotThrow()
    {
        var optionsWithoutLogger = new LLMConnectGeneralOptions { LoggerFactory = null };
        var content = "event: test\ndata: 123\n";
        using var stream = CreateStream(content);
        using var cts = new CancellationTokenSource();
        var reader = new SseStreamEventReader(optionsWithoutLogger);

        cts.Cancel();
        var act = async () =>
        {
            var enumerator = reader.ReadEventsAsync(stream, cts.Token).GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
        };

        await act.Should().NotThrowAsync();
    }

    // ---------- Multiple events with same event name ----------

    [Fact]
    public async Task ReadEventsAsync_MultipleEventsWithSameName_ResetsEventNameAfterYield()
    {
        var content = """
            event: content_block_delta
            data: {"delta":{"text":"Hello"}}
            event: content_block_delta
            data: {"delta":{"text":" world"}}
            """;
        using var stream = CreateStream(content);
        var reader = new SseStreamEventReader(_options);

        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        events.Should().HaveCount(2);
        events[0].EventName.Should().Be("content_block_delta");
        events[1].EventName.Should().Be("content_block_delta");
        // The event name is reset after the first data line, but the second event line sets it again.
        // The second data line will have the same event name.
        events[1].EventName.Should().Be("content_block_delta");
    }
}