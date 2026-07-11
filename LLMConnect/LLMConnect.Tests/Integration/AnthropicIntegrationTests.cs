using FluentAssertions;
using LLMConnect.Exceptions;
using LLMConnect.Models;
using System.Net;
using System.Text;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace LLMConnect.Tests.Integration;

public class AnthropicIntegrationTests : IntegrationTestBase
{
    public AnthropicIntegrationTests() : base(ProviderType.Anthropic) { }

    // ---------- Stubs ----------
    private void StubChat(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
        => _server
            .Given(Request.Create().WithPath("/v1/messages").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

    private void StubStream(string[] chunks, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var sb = new StringBuilder();
        foreach (var chunk in chunks)
        {
            sb.AppendLine("event: content_block_delta");
            sb.AppendLine($"data: {chunk}");
            sb.AppendLine();
        }
        sb.AppendLine("event: message_stop");
        sb.AppendLine("data: {}");
        sb.AppendLine();

        _server
            .Given(Request.Create().WithPath("/v1/messages").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "text/event-stream")
                .WithBody(sb.ToString()));
    }

    // ---------- Tests ----------

    [Fact]
    public async Task ChatAsync_ValidResponse_ReturnsChatResponse()
    {
        var json = """
        {
            "id": "msg_123",
            "model": "claude-3-5-sonnet-20241022",
            "stop_reason": "end_turn",
            "content": [{ "type": "text", "text": "Hello from Anthropic!" }],
            "usage": { "input_tokens": 10, "output_tokens": 5 }
        }
        """;

        StubChat(json);

        var result = await _client.ChatAsync(CreateChatRequest());

        result.Content.Should().Be("Hello from Anthropic!");
        result.FinishReason.Should().Be("end_turn");
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
    }

    [Fact]
    public async Task StreamAsync_ValidResponse_ReturnsChunks()
    {
        var chunks = new[]
        {
            @"{""delta"":{""text"":""Hello""}}",
            @"{""delta"":{""text"":"" from Anthropic!""}}"
        };

        StubStream(chunks);

        var result = await _client.StreamAsync(CreateChatRequest()).ToListAsync();

        result.Should().HaveCount(2);
        result[0].Content.Should().Be("Hello");
        result[1].Content.Should().Be(" from Anthropic!");
    }

    [Fact]
    public async Task ChatAsync_Error_ThrowsLLMConnectException()
    {
        StubChat(@"{""error"":{""message"":""Invalid API key""}}", HttpStatusCode.Unauthorized);

        var act = async () => await _client.ChatAsync(CreateChatRequest());

        var ex = await act.Should().ThrowAsync<LLMConnectException>();

        ex.Which.Provider.Should().Be("Anthropic");
        ex.Which.Message.Should().Be("Invalid API key");
    }

    [Fact]
    public async Task ChatAsync_Retry_SucceedsAfter429()
    {
        var fail = @"{""error"":{""message"":""Rate limit""}}";
        var success = """
        {
            "id": "msg_retry",
            "model": "claude-3-5-sonnet-20241022",
            "stop_reason": "end_turn",
            "content": [{ "type": "text", "text": "Retry worked!" }],
            "usage": { "input_tokens": 5, "output_tokens": 2 }
        }
        """;

        StubRetryScenario("/v1/messages", fail, success);

        var result = await _client.ChatAsync(CreateChatRequest());

        result.Content.Should().Be("Retry worked!");
    }

    // Anthropic does not support embeddings – no test needed
}