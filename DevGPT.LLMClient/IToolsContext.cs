public interface IToolsContext
{
    List<DevGPTChatTool> Tools { get; set; }
    void Add(DevGPTChatTool info);
}
