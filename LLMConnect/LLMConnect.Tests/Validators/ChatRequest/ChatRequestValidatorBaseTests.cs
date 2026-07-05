using FluentAssertions;
using LLMConnect.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMConnect.Tests.Validation;

public class ChatRequestValidatorBaseTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly TestChatRequestValidator _validator;

    public ChatRequestValidatorBaseTests()
    {
        _loggerMock = new Mock<ILogger>();
        _validator = new TestChatRequestValidator();
    }

    // Test subclass that implements the abstract method
    private class TestChatRequestValidator : ChatRequestValidatorBase
    {
        public bool ProviderSpecificCalled { get; private set; }

        protected override void ValidateProviderSpecific(ChatRequest request, ILogger? logger)
        {
            ProviderSpecificCalled = true;
            // No additional validation for testing
        }
    }

    // ---------- Request Null ----------

    [Fact]
    public void Validate_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _validator.Validate(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("request");
    }

    // ---------- Messages Validation ----------

    [Fact]
    public void Validate_WhenMessagesIsNull_ThrowsArgumentException()
    {
        var request = new ChatRequest { Messages = null! };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Messages))
            .WithMessage("At least one message is required.*");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("At least one message is required")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenMessagesIsEmpty_ThrowsArgumentException()
    {
        var request = new ChatRequest { Messages = new List<Message>() };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Messages));
    }

    // ---------- Temperature Validation ----------

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Validate_WhenTemperatureOutOfRange_ThrowsArgumentException(float temperature)
    {
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            Temperature = temperature
        };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Temperature))
            .WithMessage("Temperature must be between 0 and 1.*");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Temperature must be between 0 and 1")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ---------- MaxTokens Validation ----------

    [Fact]
    public void Validate_WhenMaxTokensLessThanOne_ThrowsArgumentException()
    {
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            MaxTokens = 0
        };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.MaxTokens))
            .WithMessage("MaxTokens must be greater than 0.*");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MaxTokens must be greater than 0")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ---------- StopSequences Validation ----------

    [Fact]
    public void Validate_WhenStopSequencesContainsEmptyString_ThrowsArgumentException()
    {
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            StopSequences = new List<string> { "stop", "" }
        };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.StopSequences))
            .WithMessage("StopSequences cannot contain empty or whitespace strings.*");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("StopSequences cannot contain empty or whitespace strings")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WhenStopSequencesContainsWhitespace_ThrowsArgumentException()
    {
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            StopSequences = new List<string> { " ", "\t" }
        };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.StopSequences));
    }

    // ---------- Tools Validation ----------

    [Fact]
    public void Validate_WhenToolNameEmpty_ThrowsArgumentException()
    {
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            Tools = new List<Tool>
            {
                new Tool { Name = "", Description = "desc", Parameters = new Dictionary<string, JsonSchema>() }
            }
        };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Tools))
            .WithMessage("Tool name cannot be empty.*");
    }

    [Fact]
    public void Validate_WhenToolDescriptionEmpty_ThrowsArgumentException()
    {
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            Tools = new List<Tool>
            {
                new Tool { Name = "test", Description = "", Parameters = new Dictionary<string, JsonSchema>() }
            }
        };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Tools))
            .WithMessage("Tool 'test' must have a description.*");
    }

    [Fact]
    public void Validate_WhenToolParametersNull_ThrowsArgumentException()
    {
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            Tools = new List<Tool>
            {
                new Tool { Name = "test", Description = "desc", Parameters = null! }
            }
        };
        Action act = () => _validator.Validate(request, _loggerMock.Object);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Tools))
            .WithMessage("Tool 'test' must define parameters.*");
    }

    [Fact]
    public void Validate_WhenToolParametersEmpty_ThrowsArgumentException()
    {
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            Tools = new List<Tool>
            {
                new Tool { Name = "test", Description = "desc", Parameters = new Dictionary<string, JsonSchema>() }
            }
        };
        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Tools))
            .WithMessage("Tool 'test' must define parameters.*");
    }

    [Fact]
    public void Validate_WhenToolParameterTypeEmpty_ThrowsArgumentException()
    {
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            Tools = new List<Tool>
            {
                new Tool
                {
                    Name = "test",
                    Description = "desc",
                    Parameters = new Dictionary<string, JsonSchema>
                    {
                        ["location"] = new JsonSchema { Type = "" }
                    }
                }
            }
        };

        Action act = () => _validator.Validate(request, _loggerMock.Object);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Tools)).Which.Message.Should().Be("Tool 'test' parameter 'location' -> value, must have a type. (Parameter 'Tools')");
    }

    // ---------- Valid Request ----------

    [Fact]
    public void Validate_WhenAllValid_CallsProviderSpecificValidation()
    {
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            Temperature = 0.5f,
            MaxTokens = 10,
            Tools = new List<Tool>
            {
                new Tool
                {
                    Name = "get_weather",
                    Description = "Get weather",
                    Parameters = new Dictionary<string, JsonSchema>
                    {
                        ["location"] = new JsonSchema { Type = "string" }
                    }
                }
            }
        };
        _validator.Validate(request, _loggerMock.Object);
        _validator.ProviderSpecificCalled.Should().BeTrue();
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ---------- Logger Null ----------

    [Fact]
    public void Validate_WhenLoggerIsNull_DoesNotThrow()
    {
        var request = new ChatRequest
        {
            Messages = new List<Message> { new UserMessage("Hi") },
            Temperature = 0.5f,
            MaxTokens = 10
        };
        Action act = () => _validator.Validate(request, null);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenInvalidAndLoggerIsNull_StillThrows()
    {
        var request = new ChatRequest { Messages = null! };
        Action act = () => _validator.Validate(request, null);
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ChatRequest.Messages));
    }
}