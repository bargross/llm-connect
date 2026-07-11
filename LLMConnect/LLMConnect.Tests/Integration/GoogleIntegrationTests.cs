using FluentAssertions;
using LLMConnect.Exceptions;
using LLMConnect.Models;
using System.Net;
using System.Text;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace LLMConnect.Tests.Integration;

public class GoogleIntegrationTests : IntegrationTestBase
{
    private const string Model = "gemini-3.5-flash";
    private const string ChatPath = $"/v1beta/models/{Model}:generateContent";
    private const string StreamPath = $"/v1beta/models/{Model}:streamGenerateContent";
    private const string EmbedPath = $"/v1beta/models/{Model}:embedContent";

    public GoogleIntegrationTests() : base(ProviderType.Google) { }

    private void StubChat(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
        => _server
            .Given(Request.Create().WithPath(ChatPath).UsingPost())
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

        _server
            .Given(Request.Create().WithPath(StreamPath).WithParam("alt", "sse").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "text/event-stream")
                .WithBody(sb.ToString()));
    }

    private void StubEmbeddings(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
        => _server
            .Given(Request.Create().WithPath(EmbedPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

    [Fact]
    public async Task ChatAsync_ValidResponse_ReturnsChatResponse()
    {
        var json = """
        {
            "candidates": [{ "content": { "parts": [{ "text": "Hello from Google!" }] }, "finishReason": "STOP" }],
            "usageMetadata": { "promptTokenCount": 10, "candidatesTokenCount": 5 }
        }
        """;
        StubChat(json);

        var result = await _client.ChatAsync(CreateChatRequest());
        result.Content.Should().Be("Hello from Google!");
        result.FinishReason.Should().Be("STOP");
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(5);
    }

    [Fact]
    public async Task ChatAsync_Error_ThrowsLLMConnectException()
    {
        StubChat(@"{""error"":{""message"":""Invalid API key""}}", HttpStatusCode.Unauthorized);

        Func<Task> act = async () => await _client.ChatAsync(CreateChatRequest());
        var ex = await act.Should().ThrowAsync<LLMConnectException>();
        ex.Which.Provider.Should().Be("Google");
        ex.Which.Message.Should().Be("Invalid API key");
    }

    [Fact]
    public async Task ChatAsync_Retry_SucceedsAfter429()
    {
        StubRetryScenario(ChatPath, @"{""error"":{""message"":""Rate limit""}}", """
        {
            "candidates": [{ "content": { "parts": [{ "text": "Retry worked!" }] } }],
            "usageMetadata": { "promptTokenCount": 5, "candidatesTokenCount": 2 }
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
            "candidates": [{
                "content": { "parts": [{ "functionCall": { "name": "get_weather", "args": { "location": "Boston" } } }] },
                "finishReason": "STOP"
            }]
        }
        """;
        StubChat(json);

        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Weather?") },
            Tools = new List<Tool> { tool }
        };
        var response = await _client.ChatAsync(request);
        response.ToolCalls.Should().HaveCount(1);
        var tc = response.ToolCalls[0];
        tc.Name.Should().Be("get_weather");
        tc.Arguments["location"].ToString().Should().Be("Boston");
        response.FinishReason.Should().Be("STOP");
    }

    [Fact]
    public async Task ChatAsync_WithToolChoiceRequired_SendsCorrectJson()
    {
        // Use a regex matcher to match any model in the path
        _server
            .Given(Request.Create()
                .WithPath(new WireMock.Matchers.RegexMatcher("/v1beta/models/.*:generateContent"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"ok\"}]}}]}"));

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
        body.Should().Contain("\"toolConfig\":{\"function_calling_config\":{\"mode\":\"ANY\"}}");
    }

    [Fact]
    public async Task StreamAsync_ValidResponse_ReturnsChunks()
    {
        var chunks = new[]
        {
            @"{""candidates"":[{""content"":{""parts"":[{""text"":""Hello""}]}}]}",
            @"{""candidates"":[{""content"":{""parts"":[{""text"":"" world""}]}}]}",
            @"{""candidates"":[{""finishReason"":""STOP""}]}"
        };
        StubStream(chunks);

        var result = await _client.StreamAsync(CreateChatRequest()).ToListAsync();
        result.Should().HaveCount(3);
        result[0].Content.Should().Be("Hello");
        result[1].Content.Should().Be(" world");
        result[2].IsComplete.Should().BeTrue();
        result[2].FinishReason.Should().Be("STOP");
    }

    [Fact]
    public async Task StreamAsync_Retry_SucceedsAfter429()
    {
        var fail = @"{""error"":{""message"":""Rate limit""}}";
        var success = "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Retry\"}]}}]}\n";
        StubRetryScenario(
            StreamPath,
            fail,
            success,
            configureRequest: req => req.WithParam("alt", "sse")
        );

        var result = await _client.StreamAsync(CreateChatRequest()).ToListAsync();
        result.Should().HaveCount(1);
        result[0].Content.Should().Be("Retry");
    }

    [Fact]
    public async Task EmbeddingsAsync_ValidResponse_ReturnsEmbeddingResponse()
    {
        var json = """
        {
            "embedding": { "values": [0.5, 0.6, 0.7] }
        }
        """;
        StubEmbeddings(json);

        var result = await _client.GetEmbeddingAsync(CreateEmbeddingRequest());
        result.Embedding.Should().BeEquivalentTo(new float[] { 0.5f, 0.6f, 0.7f });
        result.Model.Should().Be("google");
    }

    [Fact]
    public async Task EmbeddingsAsync_Error_ThrowsLLMConnectException()
    {
        StubEmbeddings(@"{""error"":{""message"":""Invalid input""}}", HttpStatusCode.BadRequest);

        Func<Task> act = async () => await _client.GetEmbeddingAsync(CreateEmbeddingRequest());
        var ex = await act.Should().ThrowAsync<LLMConnectException>();
        ex.Which.Provider.Should().Be("Google");
        ex.Which.Message.Should().Be("Invalid input");
    }
}