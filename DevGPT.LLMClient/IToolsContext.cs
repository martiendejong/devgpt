
public interface IToolsContext
{
    List<DevGPTChatTool> Tools { get; set; }
    Action<string, string, string>? SendMessage { get; set; }

    void Add(DevGPTChatTool info);
}
