using FluentAssertions;
using LLMConnect.Exceptions;
using LLMConnect.Models;
using System.Net;
using System.Text;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace LLMConnect.Tests.Integration;

public class OpenAIIntegrationTests : IntegrationTestBase
{
    public OpenAIIntegrationTests() : base(ProviderType.OpenAI) { }

    private void StubChat(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
        => _server
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

    private void StubStream(string[] chunks, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var sb = new StringBuilder();
        foreach (var chunk in chunks)
        {
            sb.AppendLine($"data: {chunk}");
            sb.AppendLine();
        }
        sb.AppendLine("data: [DONE]");
        sb.AppendLine();

        _server
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "text/event-stream")
                .WithBody(sb.ToString()));
    }

    private void StubEmbeddings(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
        => _server
            .Given(Request.Create().WithPath("/v1/embeddings").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

    [Fact]
    public async Task ChatAsync_ValidResponse_ReturnsChatResponse()
    {
        var json = """
        {
            "id": "chatcmpl-123",
            "model": "gpt-3.5-turbo",
            "choices": [{ "message": { "content": "Hello from OpenAI!" }, "finish_reason": "stop" }],
            "usage": { "prompt_tokens": 10, "completion_tokens": 5 }
        }
        """;
        StubChat(json);

        var result = await _client.ChatAsync(CreateChatRequest());
        result.Content.Should().Be("Hello from OpenAI!");
        result.FinishReason.Should().Be("stop");
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
    }

    [Fact]
    public async Task ChatAsync_Error_ThrowsLLMConnectException()
    {
        StubChat(@"{""error"":{""message"":""Invalid API key""}}", HttpStatusCode.Unauthorized);

        Func<Task> act = async () => await _client.ChatAsync(CreateChatRequest());
        var ex = await act.Should().ThrowAsync<LLMConnectException>();
        ex.Which.Provider.Should().Be("OpenAI");
        ex.Which.Message.Should().Be("Invalid API key");
    }

    [Fact]
    public async Task ChatAsync_Retry_SucceedsAfter429()
    {
        var fail = @"{""error"":{""message"":""Rate limit""}}";
        var success = """
        {
            "choices": [{ "message": { "content": "Retry worked!" } }],
            "usage": { "prompt_tokens": 5, "completion_tokens": 2 }
        }
        """;
        StubRetryScenario("/v1/chat/completions", fail, success);

        var result = await _client.ChatAsync(CreateChatRequest());
        result.Content.Should().Be("Retry worked!");
    }

    [Fact]
    public async Task ChatAsync_WithTools_ReturnsToolCalls()
    {
        var tool = new Tool
        {
            Name = "get_weather",
            Description = "Get current weather",
            Parameters = new Dictionary<string, JsonSchema>
            {
                ["location"] = new JsonSchema { Type = "string" }
            },
            Required = new List<string> { "location" }
        };

        var json = """
        {
            "choices": [{
                "message": {
                    "content": null,
                    "tool_calls": [{
                        "id": "call_123",
                        "function": { "name": "get_weather", "arguments": "{\"location\":\"Boston\"}" }
                    }]
                },
                "finish_reason": "tool_calls"
            }]
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
        tc.Id.Should().Be("call_123");
        tc.Name.Should().Be("get_weather");
        tc.Arguments["location"].ToString().Should().Be("Boston");
        response.FinishReason.Should().Be("tool_calls");
    }

    [Fact]
    public async Task ChatAsync_WithToolChoiceRequired_SendsCorrectJson()
    {
        _server
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody("{\"choices\":[{\"message\":{\"content\":\"ok\"}}]}"));

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
        body.Should().Contain("\"tool_choice\":\"required\"");
    }

    [Fact]
    public async Task StreamAsync_ValidResponse_ReturnsChunks()
    {
        var chunks = new[]
        {
            @"{""choices"":[{""delta"":{""content"":""Hello""}}]}",
            @"{""choices"":[{""delta"":{""content"":"" world""}}]}"
        };
        StubStream(chunks);

        var result = await _client.StreamAsync(CreateChatRequest()).ToListAsync();
        result.Should().HaveCount(2);
        result[0].Content.Should().Be("Hello");
        result[1].Content.Should().Be(" world");
    }

    [Fact]
    public async Task StreamAsync_Retry_SucceedsAfter429()
    {
        var fail = @"{""error"":{""message"":""Rate limit""}}";
        var success = "data: {\"choices\":[{\"delta\":{\"content\":\"Retry\"}}]}\n\ndata: [DONE]\n";
        StubRetryScenario("/v1/chat/completions", fail, success);

        var result = await _client.StreamAsync(CreateChatRequest()).ToListAsync();
        result.Should().HaveCount(1);
        result[0].Content.Should().Be("Retry");
    }

    [Fact]
    public async Task EmbeddingsAsync_ValidResponse_ReturnsEmbeddingResponse()
    {
        var json = """
        {
            "data": [{ "embedding": [0.1, 0.2, 0.3] }],
            "model": "text-embedding-ada-002",
            "usage": { "prompt_tokens": 10 }
        }
        """;
        StubEmbeddings(json);

        var result = await _client.GetEmbeddingAsync(CreateEmbeddingRequest());
        result.Embedding.Should().BeEquivalentTo(new float[] { 0.1f, 0.2f, 0.3f });
        result.Model.Should().Be("text-embedding-ada-002");
        result.Usage.InputTokens.Should().Be(10);
    }

    [Fact]
    public async Task EmbeddingsAsync_Error_ThrowsLLMConnectException()
    {
        StubEmbeddings(@"{""error"":{""message"":""Invalid input""}}", HttpStatusCode.BadRequest);

        Func<Task> act = async () => await _client.GetEmbeddingAsync(CreateEmbeddingRequest());
        var ex = await act.Should().ThrowAsync<LLMConnectException>();
        ex.Which.Provider.Should().Be("OpenAI");
        ex.Which.Message.Should().Be("Invalid input");
    }
}