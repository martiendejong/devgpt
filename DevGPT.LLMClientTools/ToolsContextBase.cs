using System.Text.Json;
using Store.OpnieuwOpnieuw.AIClient;

public class ToolsContextBase : IToolsContext
{
    public List<Tool> Tools { get; set; } = new List<Tool>();

    public void Add(ToolInfo info)
    {
        var chatTool = CreateDefinition(info.Name, info.Description, info.Parameters);
        var tool = new Tool { Definition = chatTool, FunctionName = info.Name, Execute = info.Execute };
        if (Tools.Any(t => t.FunctionName == tool.FunctionName)) return;
        Tools.Add(tool);
    }

    public void Add(string name, string description, List<ChatToolParameter> parameters, Func<List<DevGPTChatMessage>, DevGPTChatToolCall, Task<string>> execute)
    {
        var chatTool = CreateDefinition(name, description, parameters);
        var tool = new Tool { Definition = chatTool, FunctionName = name, Execute = execute };
        Tools.Add(tool);
    }

    public static DevGPTChatTool CreateDefinition(string name, string description, List<ChatToolParameter> parameters) => new DevGPTChatTool( name, description, parameters);
}
