using FluentAssertions;
using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect.Tests.Validators.ChatRequest;

public class GoogleChatRequestValidatorTests
{
    private readonly GoogleChatRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_DoesNotThrow()
    {
        var req = new Models.ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            Model = "gemini-3.5-flash"
        };
        _validator.Validate(req, null);
    }

    [Fact]
    public void Validate_WithEmptyMessages_Throws()
    {
        var req = new Models.ChatRequest
        {
            Messages = new List<Message>(),
            Model = "gemini"
        };
        Assert.Throws<ArgumentException>(() => _validator.Validate(req, null));
    }
}