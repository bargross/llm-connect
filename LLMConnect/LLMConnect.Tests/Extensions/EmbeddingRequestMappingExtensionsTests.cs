using FluentAssertions;
using LLMConnect.Models;

namespace LLMConnect.Tests.MappingExtensions;

public class EmbeddingRequestMappingExtensionsTests
{
    // ---------- ToOpenAIRequest ----------

    [Fact]
    public void ToOpenAIRequest_WhenRequestNull_ReturnsNull()
    {
        var result = ((EmbeddingRequest?)null).ToOpenAIRequest();
        result.Should().BeNull();
    }

    [Fact]
    public void ToOpenAIRequest_WithValidRequest_MapsCorrectly()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello world",
            Model = "custom-model",
            EncodingFormat = "base64",
            Dimensions = 256,
            User = "test-user"
        };

        var result = request.ToOpenAIRequest();

        result.Should().NotBeNull();
        result!.Model.Should().Be("custom-model");
        result.Input.Should().Be("Hello world");
        result.EncodingFormat.Should().Be("base64");
        result.Dimensions.Should().Be(256);
        result.User.Should().Be("test-user");
    }

    [Fact]
    public void ToOpenAIRequest_WhenModelNotSet_UsesDefault()
    {
        var request = new EmbeddingRequest { Text = "Hello" };
        var result = request.ToOpenAIRequest(defaultModel: "text-embedding-3-small");
        result!.Model.Should().Be("text-embedding-3-small");
    }

    [Fact]
    public void ToOpenAIRequest_WhenTextIsNull_UsesEmptyString()
    {
        var request = new EmbeddingRequest { Text = null! };
        var result = request.ToOpenAIRequest();
        result!.Input.Should().BeEmpty();
    }

    [Fact]
    public void ToOpenAIRequest_ExtraParameters_OverrideProperties()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            EncodingFormat = "float",
            Dimensions = 512,
            ExtraParameters = new Dictionary<string, object>
            {
                ["encoding_format"] = "base64",
                ["dimensions"] = 128,
                ["user"] = "override-user"
            }
        };

        var result = request.ToOpenAIRequest();
        result!.EncodingFormat.Should().Be("base64");
        result.Dimensions.Should().Be(128);
        result.User.Should().Be("override-user");
    }

    // ---------- ToGoogleRequest ----------

    [Fact]
    public void ToGoogleRequest_WhenRequestNull_ReturnsNull()
    {
        var result = ((EmbeddingRequest?)null).ToGoogleRequest();
        result.Should().BeNull();
    }

    [Fact]
    public void ToGoogleRequest_WithValidRequest_MapsCorrectly()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello Google",
            TaskType = "RETRIEVAL_DOCUMENT",
            Title = "Doc Title",
            Role = "user"
        };

        var result = request.ToGoogleRequest();
        result.Should().NotBeNull();
        result!.Content.Parts.Should().HaveCount(1);
        result.Content.Parts[0].Text.Should().Be("Hello Google");
        result.Content.Role.Should().Be("user");
        result.TaskType.Should().Be("RETRIEVAL_DOCUMENT");
        result.Title.Should().Be("Doc Title");
    }

    [Fact]
    public void ToGoogleRequest_WhenTextIsNull_UsesEmptyString()
    {
        var request = new EmbeddingRequest { Text = null! };
        var result = request.ToGoogleRequest();
        result!.Content.Parts[0].Text.Should().BeEmpty();
    }

    [Fact]
    public void ToGoogleRequest_ExtraParameters_OverrideProperties()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            ExtraParameters = new Dictionary<string, object>
            {
                ["taskType"] = "RETRIEVAL_QUERY",
                ["title"] = "Override Title",
                ["role"] = "model"
            }
        };

        var result = request.ToGoogleRequest();
        result!.TaskType.Should().Be("RETRIEVAL_QUERY");
        result.Title.Should().Be("Override Title");
        result.Content.Role.Should().Be("model");
    }

    // ---------- ToOllamaRequest ----------

    [Fact]
    public void ToOllamaRequest_WhenRequestNull_ReturnsNull()
    {
        var result = ((EmbeddingRequest?)null).ToOllamaRequest();
        result.Should().BeNull();
    }

    [Fact]
    public void ToOllamaRequest_WithValidRequest_MapsCorrectly()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello Ollama",
            Model = "nomic-embed-text"
        };

        var result = request.ToOllamaRequest();
        result.Should().NotBeNull();
        result!.Model.Should().Be("nomic-embed-text");
        result.Prompt.Should().Be("Hello Ollama");
        result.Options.Should().BeNull();
    }

    [Fact]
    public void ToOllamaRequest_WhenModelNotSet_UsesDefault()
    {
        var request = new EmbeddingRequest { Text = "Hello" };
        var result = request.ToOllamaRequest(defaultModel: "nomic-embed-text");
        result!.Model.Should().Be("nomic-embed-text");
    }

    [Fact]
    public void ToOllamaRequest_WhenTextIsNull_UsesNull()
    {
        var request = new EmbeddingRequest { Text = null! };
        var result = request.ToOllamaRequest();
        result!.Prompt.Should().BeNull();
    }

    [Fact]
    public void ToOllamaRequest_ExtraParameters_PassedAsOptions()
    {
        var request = new EmbeddingRequest
        {
            Text = "Hello",
            ExtraParameters = new Dictionary<string, object>
            {
                ["num_ctx"] = 2048,
                ["temperature"] = 0.5
            }
        };

        var result = request.ToOllamaRequest();
        result!.Options.Should().ContainKeys("num_ctx", "temperature");
        result.Options["num_ctx"].Should().Be(2048);
        result.Options["temperature"].Should().Be(0.5);
    }
}