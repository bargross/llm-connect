using FluentAssertions;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace LLMConnect.Tests.Streams.StreamReaders;

public class NdjsonStreamEventReaderTests
{
    private readonly Mock<ILogger<NdjsonStreamEventReader>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LLMConnectGeneralOptions _options;

    public NdjsonStreamEventReaderTests()
    {
        _loggerMock = new Mock<ILogger<NdjsonStreamEventReader>>();
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

    // ---------- OpenAI-style "data: " prefix ----------

    [Fact]
    public async Task ReadEventsAsync_WithDataPrefixLines_ReturnsEvents()
    {
        var content = """
            data: {"id":"1","content":"Hello"}
            data: {"id":"2","content":" world"}
            """;
        using var stream = CreateStream(content);
        var reader = new NdjsonStreamEventReader(_options);

        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        events.Should().HaveCount(2);
        events[0].EventName.Should().BeNull();
        events[0].Data.Should().Be(@"{""id"":""1"",""content"":""Hello""}");
        events[1].EventName.Should().BeNull();
        events[1].Data.Should().Be(@"{""id"":""2"",""content"":"" world""}");
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Ollama-style raw JSON lines ----------

    [Fact]
    public async Task ReadEventsAsync_WithRawJsonLines_ReturnsEvents()
    {
        var content = """
            {"id":"1","content":"Hello"}
            {"id":"2","content":" world"}
            """;
        using var stream = CreateStream(content);
        var reader = new NdjsonStreamEventReader(_options);

        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        events.Should().HaveCount(2);
        events[0].Data.Should().Be(@"{""id"":""1"",""content"":""Hello""}");
        events[1].Data.Should().Be(@"{""id"":""2"",""content"":"" world""}");
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Mixed prefix and raw ----------

    [Fact]
    public async Task ReadEventsAsync_WithMixedPrefixAndRaw_ReturnsAllEvents()
    {
        var content = """
            data: {"type":"openai"}
            {"type":"ollama"}
            data: {"type":"openai2"}
            """;
        using var stream = CreateStream(content);
        var reader = new NdjsonStreamEventReader(_options);

        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        events.Should().HaveCount(3);
        events[0].Data.Should().Be(@"{""type"":""openai""}");
        events[1].Data.Should().Be(@"{""type"":""ollama""}");
        events[2].Data.Should().Be(@"{""type"":""openai2""}");
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Done sentinel ----------

    [Fact]
    public async Task ReadEventsAsync_WithDoneSentinel_StopsAfterSentinel()
    {
        var content = """
            data: {"id":"1"}
            data: [DONE]
            data: {"id":"2"}
            """;
        using var stream = CreateStream(content);
        var reader = new NdjsonStreamEventReader(_options);

        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        events.Should().HaveCount(2);
        events[0].Data.Should().Be(@"{""id"":""1""}");
        events[1].Data.Should().Be("[DONE]");
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Empty lines ----------

    [Fact]
    public async Task ReadEventsAsync_WithEmptyLines_SkipsThem()
    {
        var content = """

            data: {"id":"1"}

            {"id":"2"}

            """.TrimStart(); // Keep leading newline
        using var stream = CreateStream(content);
        var reader = new NdjsonStreamEventReader(_options);

        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        events.Should().HaveCount(2);
        events[0].Data.Should().Be(@"{""id"":""1""}");
        events[1].Data.Should().Be(@"{""id"":""2""}");
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Cancellation ----------

    [Fact]
    public async Task ReadEventsAsync_WithCancellation_LogsAndBreaks()
    {
        var content = """
            data: {"id":"1"}
            data: {"id":"2"}
            """;
        using var stream = CreateStream(content);
        using var cts = new CancellationTokenSource();
        var reader = new NdjsonStreamEventReader(_options);

        // Cancel after first event
        var enumerator = reader.ReadEventsAsync(stream, cts.Token).GetAsyncEnumerator();
        var hasNext = await enumerator.MoveNextAsync();
        hasNext.Should().BeTrue();
        enumerator.Current.Data.Should().Be(@"{""id"":""1""}");

        // Cancel and try to get next
        cts.Cancel();
        var nextHasNext = await enumerator.MoveNextAsync();
        nextHasNext.Should().BeFalse();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAI stream has ended.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ---------- No data ----------

    [Fact]
    public async Task ReadEventsAsync_WithNoData_ReturnsEmpty()
    {
        using var stream = new MemoryStream();
        var reader = new NdjsonStreamEventReader(_options);

        var events = await reader.ReadEventsAsync(stream).ToListAsync();

        events.Should().BeEmpty();
        _loggerMock.VerifyNoOtherCalls();
    }

    // ---------- Logger null ----------

    [Fact]
    public async Task ReadEventsAsync_WhenLoggerIsNull_DoesNotThrow()
    {
        var optionsWithoutLogger = new LLMConnectGeneralOptions { LoggerFactory = null };
        var content = "data: {\"test\":\"value\"}";
        using var stream = CreateStream(content);
        var reader = new NdjsonStreamEventReader(optionsWithoutLogger);

        var act = async () => await reader.ReadEventsAsync(stream).ToListAsync();

        await act.Should().NotThrowAsync();
    }

    // ---------- Cancellation without logger ----------

    [Fact]
    public async Task ReadEventsAsync_WithCancellationAndNoLogger_DoesNotThrow()
    {
        var optionsWithoutLogger = new LLMConnectGeneralOptions { LoggerFactory = null };
        var content = "data: {\"test\":\"value\"}";
        using var stream = CreateStream(content);
        using var cts = new CancellationTokenSource();
        var reader = new NdjsonStreamEventReader(optionsWithoutLogger);

        cts.Cancel();
        var act = async () =>
        {
            var enumerator = reader.ReadEventsAsync(stream, cts.Token).GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
        };

        await act.Should().NotThrowAsync();
    }
}