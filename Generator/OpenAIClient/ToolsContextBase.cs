﻿using System.Text.Json;
using OpenAI.Chat;

public class ToolsContextBase : IToolsContext
{
    public List<Tool> Tools { get; set; } = new List<Tool>();

    public void Add(string name, string description, List<ChatToolParameter> parameters, Func<List<ChatMessage>, ChatToolCall, Task<string>> execute)
    {
        var chatYool = CreateDefinition(name, description, parameters);
        var tool = new Tool { Definition = chatYool, FunctionName = name, Execute = execute };
        Tools.Add(tool);
    }

    public static ChatTool CreateDefinition(string name, string description, List<ChatToolParameter> parameters)
    {
        var d = ChatTool.CreateFunctionTool(
            functionName: name,
            functionDescription: description,
            functionParameters: GenerateFunctionParameters(parameters));
        return d;
    }

    protected static BinaryData GenerateFunctionParameters(List<ChatToolParameter> parameters)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var prop in parameters)
        {
            properties[prop.Name] = new Dictionary<string, object>
            {
                ["type"] = prop.Type,
                ["description"] = prop.Description
            };

            if(prop.Required)
                required.Add(prop.Name);
        }

        var schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required
        };

        string json = JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
        return BinaryData.FromString(json);
    }
}
