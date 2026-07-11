using FluentAssertions;
using LLMConnect.Models;

namespace LLMConnect.Tests.MappingExtensions;

public class ToolMappingExtensionsTests
{
    // ---------- BuildParametersDictionary ----------

    [Fact]
    public void BuildParametersDictionary_WithPropertiesAndRequired_CreatesCorrectDictionary()
    {
        var tool = new Tool
        {
            Parameters = new Dictionary<string, JsonSchema>
            {
                { "location", new JsonSchema { Type = "string", Description = "City name" } }
            },
            Required = new List<string> { "location" }
        };

        var result = ToolMappingExtensions.BuildParametersDictionary(tool);

        result.Should().ContainKey("type").WhoseValue.Should().Be("object");
        result.Should().ContainKey("properties");

        var props = (Dictionary<string, Dictionary<string, object>>)result["properties"];
        props.Should().ContainKey("location");

        var loc = (Dictionary<string, object>)props["location"];
        loc.Should().ContainKey("type").WhoseValue.Should().Be("string");
        loc.Should().ContainKey("description").WhoseValue.Should().Be("City name");
        result.Should().ContainKey("required").WhoseValue.Should().BeEquivalentTo(new List<string> { "location" });
    }

    [Fact]
    public void BuildParametersDictionary_WithNoRequired_OmitsRequired()
    {
        var tool = new Tool
        {
            Parameters = new Dictionary<string, JsonSchema>
            {
                { "location", new JsonSchema { Type = "string" } }
            },
            Required = null
        };

        var result = ToolMappingExtensions.BuildParametersDictionary(tool);

        result.Should().ContainKey("properties");
        result.Should().NotContainKey("required");
    }

    [Fact]
    public void BuildParametersDictionary_WithNestedProperties_RecursivelyBuilds()
    {
        var tool = new Tool
        {
            Parameters = new Dictionary<string, JsonSchema>
            {
                { "address", new JsonSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, JsonSchema>
                        {
                            { "street", new JsonSchema { Type = "string" } }
                        }
                    }
                }
            }
        };

        var result = ToolMappingExtensions.BuildParametersDictionary(tool);
        var props = (Dictionary<string, Dictionary<string, object>>)result["properties"];
        var address = (Dictionary<string, object>)props["address"];

        address.Should().ContainKey("properties");

        var addrProps = address["properties"] as Dictionary<string, object>;
        addrProps.Should().ContainKey("street");
    }

    // ---------- MapOpenAITools ----------

    [Fact]
    public void MapOpenAITools_WhenToolsNull_ReturnsNull()
    {
        var request = new ChatRequest { Tools = null };
        var result = request.MapOpenAITools();
        result.Should().BeNull();
    }

    [Fact]
    public void MapOpenAITools_WhenToolsEmpty_ReturnsNull()
    {
        var request = new ChatRequest { Tools = new List<Tool>() };
        var result = request.MapOpenAITools();
        result.Should().BeNull();
    }

    [Fact]
    public void MapOpenAITools_WithValidTools_ReturnsOpenAIToolList()
    {
        var request = new ChatRequest
        {
            Tools = new List<Tool>
            {
                new Tool
                {
                    Name = "get_weather",
                    Description = "Get weather",
                    Parameters = new Dictionary<string, JsonSchema>
                    {
                        { "location", new JsonSchema { Type = "string" } }
                    },
                    Required = new List<string> { "location" }
                }
            }
        };

        var result = request.MapOpenAITools();
        result.Should().HaveCount(1);
        var tool = result.First();
        tool.Function.Name.Should().Be("get_weather");
        tool.Function.Description.Should().Be("Get weather");
        tool.Function.Parameters.Should().ContainKey("type");
        tool.Function.Parameters.Should().ContainKey("properties");
        tool.Function.Parameters.Should().ContainKey("required");
    }

    // ---------- MapAnthropicTools ----------

    [Fact]
    public void MapAnthropicTools_WhenToolsNull_ReturnsNull()
    {
        var request = new ChatRequest { Tools = null };
        var result = request.MapAnthropicTools();
        result.Should().BeNull();
    }

    [Fact]
    public void MapAnthropicTools_WithValidTools_ReturnsAnthropicToolList()
    {
        var request = new ChatRequest
        {
            Tools = new List<Tool>
            {
                new Tool
                {
                    Name = "get_weather",
                    Description = "Get weather",
                    Parameters = new Dictionary<string, JsonSchema>
                    {
                        { "location", new JsonSchema { Type = "string" } }
                    }
                }
            }
        };

        var result = request.MapAnthropicTools();
        result.Should().HaveCount(1);
        var tool = result.First();
        tool.Name.Should().Be("get_weather");
        tool.Description.Should().Be("Get weather");
        tool.InputSchema.Should().ContainKey("type");
        tool.InputSchema.Should().ContainKey("properties");
    }

    // ---------- MapGoogleTools ----------

    [Fact]
    public void MapGoogleTools_WhenToolsNull_ReturnsNull()
    {
        var request = new ChatRequest { Tools = null };
        var result = request.MapGoogleTools();
        result.Should().BeNull();
    }

    [Fact]
    public void MapGoogleTools_WithValidTools_ReturnsGoogleToolList()
    {
        var request = new ChatRequest
        {
            Tools = new List<Tool>
            {
                new Tool
                {
                    Name = "get_weather",
                    Description = "Get weather",
                    Parameters = new Dictionary<string, JsonSchema>
                    {
                        { "location", new JsonSchema { Type = "string" } }
                    }
                }
            }
        };

        var result = request.MapGoogleTools();
        result.Should().HaveCount(1);
        var googleTool = result.First();
        googleTool.FunctionDeclarations.Should().HaveCount(1);
        var func = googleTool.FunctionDeclarations.First();
        func.Name.Should().Be("get_weather");
        func.Description.Should().Be("Get weather");
        func.Parameters.Should().ContainKey("type");
        func.Parameters.Should().ContainKey("properties");
    }

    // ---------- MapOllamaTools ----------

    [Fact]
    public void MapOllamaTools_WhenToolsNull_ReturnsNull()
    {
        var request = new ChatRequest { Tools = null };
        var result = request.MapOllamaTools();
        result.Should().BeNull();
    }

    [Fact]
    public void MapOllamaTools_WithValidTools_ReturnsOllamaToolList()
    {
        var request = new ChatRequest
        {
            Tools = new List<Tool>
            {
                new Tool
                {
                    Name = "get_weather",
                    Description = "Get weather",
                    Parameters = new Dictionary<string, JsonSchema>
                    {
                        { "location", new JsonSchema { Type = "string" } }
                    }
                }
            }
        };

        var result = request.MapOllamaTools();
        result.Should().HaveCount(1);
        var tool = result.First();
        tool.Function.Name.Should().Be("get_weather");
        tool.Function.Description.Should().Be("Get weather");
        tool.Function.Parameters.Should().ContainKey("type");
        tool.Function.Parameters.Should().ContainKey("properties");
    }

    // ---------- Tool Choice Mapping ----------

    [Theory]
    [InlineData("auto")]
    [InlineData("required")]
    [InlineData("none")]
    public void MapOpenAIToolChoice_ForReservedKeywords_ReturnsSameString(string value)
    {
        var request = new ChatRequest { ToolChoice = value };
        var result = request.MapOpenAIToolChoice();
        result.Should().Be(value);
    }

    [Fact]
    public void MapOpenAIToolChoice_ForSpecificTool_ReturnsObjectWithTypeAndFunction()
    {
        var request = new ChatRequest { ToolChoice = "get_weather" };
        var result = request.MapOpenAIToolChoice();

        // This compares the anonymous object's properties by name and value
        result.Should().BeEquivalentTo(new
        {
            type = "function",
            function = new { name = "get_weather" }
        });
    }

    [Fact]
    public void MapOpenAIToolChoice_WhenToolChoiceNullOrEmpty_ReturnsNull()
    {
        var request = new ChatRequest { ToolChoice = null };
        request.MapOpenAIToolChoice().Should().BeNull();
        request.ToolChoice = "";
        request.MapOpenAIToolChoice().Should().BeNull();
    }

    [Fact]
    public void MapAnthropicToolChoice_ReturnsToolChoiceAsIs()
    {
        var request = new ChatRequest { ToolChoice = "auto" };
        request.MapAnthropicToolChoice().Should().Be("auto");
        request.ToolChoice = "get_weather";
        request.MapAnthropicToolChoice().Should().Be("get_weather");
        request.ToolChoice = null;
        request.MapAnthropicToolChoice().Should().BeNull();
    }

    [Theory]
    [InlineData("auto", "AUTO")]
    [InlineData("required", "ANY")]
    [InlineData("none", "NONE")]
    public void MapGoogleToolChoice_ForReservedKeywords_MapsToCorrectMode(string input, string expectedMode)
    {
        var request = new ChatRequest { ToolChoice = input };
        var result = request.MapGoogleToolChoice();
        result.Should().NotBeNull();
        result.FunctionCallingConfig.Mode.Should().Be(expectedMode);
        result.FunctionCallingConfig.AllowedFunctionNames.Should().BeNull();
    }

    [Fact]
    public void MapGoogleToolChoice_ForSpecificTool_AddsAllowedFunctionNames()
    {
        var request = new ChatRequest { ToolChoice = "get_weather" };
        var result = request.MapGoogleToolChoice();
        result.FunctionCallingConfig.Mode.Should().Be("ANY");
        result.FunctionCallingConfig.AllowedFunctionNames.Should().Contain("get_weather");
    }

    [Fact]
    public void MapGoogleToolChoice_WhenToolChoiceNullOrEmpty_ReturnsNull()
    {
        var request = new ChatRequest { ToolChoice = null };
        request.MapGoogleToolChoice().Should().BeNull();
        request.ToolChoice = "";
        request.MapGoogleToolChoice().Should().BeNull();
    }

    [Fact]
    public void MapOllamaToolChoice_ReturnsToolChoiceAsIs()
    {
        var request = new ChatRequest { ToolChoice = "auto" };
        request.MapOllamaToolChoice().Should().Be("auto");
        request.ToolChoice = "get_weather";
        request.MapOllamaToolChoice().Should().Be("get_weather");
        request.ToolChoice = null;
        request.MapOllamaToolChoice().Should().BeNull();
    }
}