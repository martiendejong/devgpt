
public interface IToolsContext
{
    List<DevGPTChatTool> Tools { get; set; }
    Action<string> SendMessage { get; set; }

    void Add(DevGPTChatTool info);
}
