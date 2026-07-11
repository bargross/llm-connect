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

    [Fact]
    public async Task ChatAsync_ValidResponse_ReturnsChatResponse()
    {
        var json = """
        {
            "id": "msg_123",
            "content": [{ "type": "text", "text": "Hello from Anthropic!" }],
            "stop_reason": "end_turn",
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
    public async Task ChatAsync_Error_ThrowsLLMConnectException()
    {
        StubChat(@"{""error"":{""message"":""Invalid API key""}}", HttpStatusCode.Unauthorized);

        Func<Task> act = async () => await _client.ChatAsync(CreateChatRequest());
        var ex = await act.Should().ThrowAsync<LLMConnectException>();
        ex.Which.Provider.Should().Be("Anthropic");
        ex.Which.Message.Should().Be("Invalid API key");
    }

    [Fact]
    public async Task ChatAsync_Retry_SucceedsAfter429()
    {
        StubRetryScenario("/v1/messages", @"{""error"":{""message"":""Rate limit""}}", """
        {
            "id": "msg_retry",
            "content": [{ "type": "text", "text": "Retry worked!" }],
            "usage": { "input_tokens": 5, "output_tokens": 2 }
        }
        """);

        var result = await _client.ChatAsync(CreateChatRequest());
        result.Content.Should().Be("Retry worked!");
    }

    [Fact]
    public async Task ChatAsync_WithTools_ReturnsToolCalls()
    {
        var tool = new Tool
        {
            Name = "get_weather",
            Description = "Get the weather for a location",
            Parameters = new Dictionary<string, JsonSchema> { ["location"] = new JsonSchema { Type = "string" } }
        };
        var json = """
        {
            "id": "msg_123",
            "content": [{ "type": "tool_use", "id": "toolu_123", "name": "get_weather", "input": { "location": "Boston" } }],
            "stop_reason": "tool_use"
        }
        """;
        StubChat(json);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("What's the weather?") },
            Tools = new List<Tool> { tool }
        };

        var response = await _client.ChatAsync(request);
        response.ToolCalls.Should().HaveCount(1);
        var tc = response.ToolCalls[0];
        tc.Id.Should().Be("toolu_123");
        tc.Name.Should().Be("get_weather");
        tc.Arguments["location"].ToString().Should().Be("Boston");
        response.FinishReason.Should().Be("tool_use");
    }

    [Fact]
    public async Task ChatAsync_WithToolChoiceRequired_SendsCorrectJson()
    {
        _server
            .Given(Request.Create().WithPath("/v1/messages").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody("{\"id\":\"test\",\"content\":[{\"type\":\"text\",\"text\":\"ok\"}]}"));

        var tool = new Tool
        {
            Name = "get_weather",
            Description = "Get the weather for a location",
            Parameters = new Dictionary<string, JsonSchema> { ["location"] = new JsonSchema { Type = "string" } }
        };
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            Tools = new List<Tool> { tool },
            ToolChoice = "required"
        };

        await _client.ChatAsync(request);

        var logEntries = _server.LogEntries;
        var requestLog = logEntries.Should().ContainSingle().Subject;
        var body = requestLog.RequestMessage.Body?.ToString() ?? string.Empty;
        // The library sends "tool_choice":"required" as a string
        body.Should().Contain("\"tool_choice\":\"required\"");
    }

    [Fact]
    public async Task StreamAsync_ValidResponse_ReturnsChunks()
    {
        var chunks = new[]
        {
            @"{""delta"":{""text"":""Hello""}}",
            @"{""delta"":{""text"":"" world""}}"
        };
        StubStream(chunks);

        var result = await _client.StreamAsync(CreateChatRequest()).ToListAsync();
        result.Should().HaveCount(3);
        result[0].Content.Should().Be("Hello");
        result[1].Content.Should().Be(" world");
        result[2].IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task StreamAsync_Retry_SucceedsAfter429()
    {
        var fail = @"{""error"":{""message"":""Rate limit""}}";
        var success = "event: content_block_delta\ndata: {\"delta\":{\"text\":\"Retry\"}}\n\nevent: message_stop\ndata: {}\n";
        StubRetryScenario("/v1/messages", fail, success);

        var result = await _client.StreamAsync(CreateChatRequest()).ToListAsync();
        result.Should().HaveCount(2);
        result[0].Content.Should().Be("Retry");
    }

    [Fact]
    public async Task GetEmbeddingAsync_ThrowsNotSupportedException()
    {
        Func<Task> act = async () => await _client.GetEmbeddingAsync(CreateEmbeddingRequest());
        await act.Should().ThrowAsync<NotSupportedException>();
    }
}