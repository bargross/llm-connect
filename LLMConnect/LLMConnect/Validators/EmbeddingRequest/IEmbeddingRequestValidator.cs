using LLMConnect.Models;
using Microsoft.Extensions.Logging;

namespace LLMConnect;

internal interface IEmbeddingRequestValidator
{
    void Validate(EmbeddingRequest request, ILogger? logger = null);
}
