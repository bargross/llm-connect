using LLMConnect.Settings;
using Microsoft.Extensions.Logging;

namespace LLMConnect
{
    internal interface IChatRequestValidator
    {
        void Validate(ChatRequest request, ILogger? logger = null);
    }
}
