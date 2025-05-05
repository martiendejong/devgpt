using Store.OpnieuwOpnieuw.AIClient;

public class Tool
{
    public string FunctionName { get; set; }
    public DevGPTChatTool Definition { get; set; }
    public Func<List<DevGPTChatMessage>, DevGPTChatToolCall, Task<string>> Execute { get; set; }
}
