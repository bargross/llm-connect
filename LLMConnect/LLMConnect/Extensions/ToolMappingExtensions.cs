using LLMConnect.Models;

namespace LLMConnect;

internal static class ToolMappingExtensions
{
    // ---------- OpenAI ----------
    internal static List<OpenAITool>? MapOpenAITools(this ChatRequest request)
    {
        if (request.Tools == null || request.Tools.Count == 0)
            return null;

        return request.Tools.Select(t => new OpenAITool
        {
            Function = new OpenAIFunction
            {
                Name = t.Name,
                Description = t.Description,
                Parameters = BuildParametersDictionary(t)
            }
        }).ToList();
    }

    // ---------- Anthropic ----------
    internal static List<AnthropicTool>? MapAnthropicTools(this ChatRequest request)
    {
        if (request.Tools == null || request.Tools.Count == 0)
            return null;

        return request.Tools.Select(t => new AnthropicTool
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = BuildParametersDictionary(t)
        }).ToList();
    }

    // ---------- Google ----------
    internal static List<GoogleTool>? MapGoogleTools(this ChatRequest request)
    {
        if (request.Tools == null || request.Tools.Count == 0)
            return null;

        return new List<GoogleTool>
        {
            new GoogleTool
            {
                FunctionDeclarations = request.Tools.Select(t => new GoogleFunctionDeclaration
                {
                    Name = t.Name,
                    Description = t.Description,
                    Parameters = BuildParametersDictionary(t)
                }).ToList()
            }
        };
    }

    // ---------- Ollama ----------
    internal static List<OllamaTool>? MapOllamaTools(this ChatRequest request)
    {
        if (request.Tools == null || request.Tools.Count == 0)
            return null;

        return request.Tools.Select(t => new OllamaTool
        {
            Function = new OllamaFunction
            {
                Name = t.Name,
                Description = t.Description,
                Parameters = BuildParametersDictionary(t)
            }
        }).ToList();
    }

    // ---------- Shared helper for building parameters ----------
    internal static Dictionary<string, object> BuildParametersDictionary(Tool tool)
    {
        var dict = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = tool.Parameters.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToDictionary()
            )
        };

        if (tool.Required != null && tool.Required.Count > 0)
            dict["required"] = tool.Required;

        return dict;
    }

    // ---------- Tool Choice Mapping ----------

    internal static object? MapOpenAIToolChoice(this ChatRequest request)
    {
        if (string.IsNullOrEmpty(request.ToolChoice))
            return null;

        // Reserved keywords: "auto", "required", "none"
        if (request.ToolChoice == "auto" || request.ToolChoice == "required" || request.ToolChoice == "none")
            return request.ToolChoice;

        // Specific tool name → { "type": "function", "function": { "name": "..." } }
        return new { type = "function", function = new { name = request.ToolChoice } };
    }

    internal static string? MapAnthropicToolChoice(this ChatRequest request)
    {
        return request.ToolChoice; // Anthropic accepts "auto", "required", or tool name as a string
    }

    internal static GoogleToolConfig? MapGoogleToolChoice(this ChatRequest request)
    {
        if (string.IsNullOrEmpty(request.ToolChoice))
            return null;

        // Map to Google's modes
        var mode = request.ToolChoice switch
        {
            "auto" => "AUTO",
            "required" => "ANY",
            "none" => "NONE",
            _ => "ANY"
        };

        var config = new GoogleToolConfig
        {
            FunctionCallingConfig = new GoogleFunctionCallingConfig
            {
                Mode = mode
            }
        };

        // If a specific tool name is provided, set allowed_function_names
        if (mode == "ANY" && request.ToolChoice != "auto" && request.ToolChoice != "required" && request.ToolChoice != "none")
            config.FunctionCallingConfig.AllowedFunctionNames = new List<string> { request.ToolChoice };

        return config;
    }

    internal static string? MapOllamaToolChoice(this ChatRequest request)
    {
        return request.ToolChoice; // Ollama accepts "auto", "required", or tool name as a string
    }
}