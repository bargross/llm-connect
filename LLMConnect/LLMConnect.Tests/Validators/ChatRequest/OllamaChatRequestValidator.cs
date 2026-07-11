using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect.Tests.Validators.ChatRequest;

public class OllamaChatRequestValidatorTests
{
    private readonly OllamaChatRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_DoesNotThrow()
    {
        var req = new Models.ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") }
        };
        _validator.Validate(req, null);
    }

    [Fact]
    public void Validate_WithEmptyMessages_Throws()
    {
        var req = new Models.ChatRequest
        {
            Messages = new List<Message>()
        };
        Assert.Throws<ArgumentException>(() => _validator.Validate(req, null));
    }
}