using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal interface IChatRequestValidator
{
    void Validate(ChatRequest request, ILogger? logger = null);
}
