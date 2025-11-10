
public class ToolsContext : IToolsContext
{
    public List<DevGPTChatTool> Tools { get; set; } = new List<DevGPTChatTool>();
    public Action<string, string, string>? SendMessage { get; set; }

    public void Add(DevGPTChatTool info)
    {
        Tools.Add(info);
    }
}
