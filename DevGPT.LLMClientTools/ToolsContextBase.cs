using System.Text.Json;

public class ToolsContextBase : IToolsContext
{
    public List<DevGPTChatTool> Tools { get; set; } = new List<DevGPTChatTool>();
    public Action<string, string, string>? SendMessage { get; set; } = null;

    public void Add(DevGPTChatTool info)
    {
        if (Tools.Any(t => t.FunctionName == info.FunctionName)) return;
        Tools.Add(info);
    }

    public void Add(string name, string description, List<ChatToolParameter> parameters, Func<List<DevGPTChatMessage>, DevGPTChatToolCall, Task<string>> execute)
    {
        Add(new DevGPTChatTool(name, description, parameters, execute));
    }
}
