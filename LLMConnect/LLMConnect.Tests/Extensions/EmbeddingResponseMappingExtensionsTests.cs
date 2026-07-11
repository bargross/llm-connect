using FluentAssertions;

namespace LLMConnect.Tests.MappingExtensions;

public class EmbeddingResponseMappingExtensionsTests
{
    // ---------- OpenAI ----------

    [Fact]
    public void ToEmbeddingResponse_OpenAI_WithValidData_MapsCorrectly()
    {
        var response = new OpenAIEmbeddingResponse
        {
            Data = new List<OpenAIEmbeddingData>
            {
                new OpenAIEmbeddingData { Embedding = new float[] { 0.1f, 0.2f, 0.3f } }
            },
            Model = "text-embedding-3-small",
            Usage = new OpenAIEmbeddingUsage
            {
                PromptTokens = 10,
                TotalTokens = 10
            }
        };

        var result = response.ToEmbeddingResponse();

        result.Embedding.Should().BeEquivalentTo(new float[] { 0.1f, 0.2f, 0.3f });
        result.Model.Should().Be("text-embedding-3-small");
        result.Usage.Should().NotBeNull();
        result.Usage.InputTokens.Should().Be(10);
        result.Usage.OutputTokens.Should().Be(0);
        result.Usage.TotalTokens.Should().Be(10);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToEmbeddingResponse_OpenAI_WhenDataIsNull_ReturnsEmptyEmbedding()
    {
        var response = new OpenAIEmbeddingResponse
        {
            Data = null,
            Model = "text-embedding-3-small",
            Usage = null
        };

        var result = response.ToEmbeddingResponse();

        result.Embedding.Should().BeEmpty();
        result.Model.Should().Be("text-embedding-3-small");
        result.Usage.Should().BeNull();
    }

    [Fact]
    public void ToEmbeddingResponse_OpenAI_WhenDataListIsEmpty_ReturnsEmptyEmbedding()
    {
        var response = new OpenAIEmbeddingResponse
        {
            Data = new List<OpenAIEmbeddingData>(),
            Model = "text-embedding-3-small",
            Usage = null
        };

        var result = response.ToEmbeddingResponse();

        result.Embedding.Should().BeEmpty();
    }

    [Fact]
    public void ToEmbeddingResponse_OpenAI_WhenEmbeddingIsNull_ReturnsEmptyEmbedding()
    {
        var response = new OpenAIEmbeddingResponse
        {
            Data = new List<OpenAIEmbeddingData>
            {
                new OpenAIEmbeddingData { Embedding = null }
            },
            Model = "text-embedding-3-small",
            Usage = null
        };

        var result = response.ToEmbeddingResponse();

        result.Embedding.Should().BeEmpty();
    }

    // ---------- Google ----------

    [Fact]
    public void ToEmbeddingResponse_Google_WithValidData_MapsCorrectly()
    {
        var response = new GoogleEmbeddingResponse
        {
            Embedding = new GoogleEmbeddingData
            {
                Values = new float[] { 0.5f, 0.6f, 0.7f }
            }
        };

        var result = response.ToEmbeddingResponse();

        result.Embedding.Should().BeEquivalentTo(new float[] { 0.5f, 0.6f, 0.7f });
        result.Model.Should().Be("google");
        result.Usage.Should().BeNull();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToEmbeddingResponse_Google_WhenEmbeddingIsNull_ReturnsEmptyEmbedding()
    {
        var response = new GoogleEmbeddingResponse
        {
            Embedding = null
        };

        var result = response.ToEmbeddingResponse();

        result.Embedding.Should().BeEmpty();
        result.Model.Should().Be("google");
        result.Usage.Should().BeNull();
    }

    [Fact]
    public void ToEmbeddingResponse_Google_WhenValuesIsNull_ReturnsEmptyEmbedding()
    {
        var response = new GoogleEmbeddingResponse
        {
            Embedding = new GoogleEmbeddingData { Values = null }
        };

        var result = response.ToEmbeddingResponse();

        result.Embedding.Should().BeEmpty();
    }

    // ---------- Ollama ----------

    [Fact]
    public void ToEmbeddingResponse_Ollama_WithValidData_MapsCorrectly()
    {
        var response = new OllamaEmbeddingResponse
        {
            Embedding = new float[] { 0.9f, 1.0f, 1.1f }
        };

        var result = response.ToEmbeddingResponse();

        result.Embedding.Should().BeEquivalentTo(new float[] { 0.9f, 1.0f, 1.1f });
        result.Model.Should().Be("ollama");
        result.Usage.Should().BeNull();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToEmbeddingResponse_Ollama_WhenEmbeddingIsNull_ReturnsEmptyEmbedding()
    {
        var response = new OllamaEmbeddingResponse
        {
            Embedding = null
        };

        var result = response.ToEmbeddingResponse();

        result.Embedding.Should().BeEmpty();
        result.Model.Should().Be("ollama");
        result.Usage.Should().BeNull();
    }
}