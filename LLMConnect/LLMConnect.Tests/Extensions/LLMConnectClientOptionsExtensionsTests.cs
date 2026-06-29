using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect.Tests;

public class LLMConnectClientOptionsExtensionsTests
{
    [Theory]
    [InlineData(ProviderType.OpenAI, "gpt-3.5-turbo")]
    [InlineData(ProviderType.Anthropic, "claude-3-5-sonnet-20241022")]
    [InlineData(ProviderType.Google, "gemini-2.0-flash")]
    [InlineData(ProviderType.Ollama, "llama3.2")]
    public void InternalComputedDefaultModel_WhenDefaultModelIsNullOrWhiteSpace_ReturnsProviderDefault(ProviderType provider, string expectedDefault)
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = provider,
            DefaultModel = null // or string.Empty or whitespace
        };

        // Act
        var result = options.InternalComputedDefaultModel();

        // Assert
        result.Should().Be(expectedDefault);
    }

    [Theory]
    [InlineData(ProviderType.OpenAI)]
    [InlineData(ProviderType.Anthropic)]
    [InlineData(ProviderType.Google)]
    [InlineData(ProviderType.Ollama)]
    public void InternalComputedDefaultModel_WhenDefaultModelIsProvided_ReturnsProvidedValue(ProviderType provider)
    {
        // Arrange
        var customModel = "custom-model-123";
        var options = new LLMConnectClientOptions
        {
            Provider = provider,
            DefaultModel = customModel
        };

        // Act
        var result = options.InternalComputedDefaultModel();

        // Assert
        result.Should().Be(customModel);
    }

    [Theory]
    [InlineData("")] // Empty string
    [InlineData(" ")] // Whitespace
    [InlineData(null)] // Null
    public void InternalComputedDefaultModel_WhenDefaultModelIsEmptyOrWhitespace_ReturnsProviderDefault(string? defaultModel)
    {
        // Arrange
        var options = new LLMConnectClientOptions
        {
            Provider = ProviderType.OpenAI,
            DefaultModel = defaultModel
        };

        // Act
        var result = options.InternalComputedDefaultModel();

        // Assert
        result.Should().Be("gpt-3.5-turbo"); // OpenAI default
    }
}