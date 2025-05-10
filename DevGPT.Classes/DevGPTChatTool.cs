using DevGPT.Chat;
using Store.OpnieuwOpnieuw.AIClient;

public class DevGPTChatTool
{
    public DevGPTChatTool(string name, string description, List<ChatToolParameter> parameters, Func<List<DevGPTChatMessage>, DevGPTChatToolCall, Task<string>> execute)
    {
        FunctionName = name;
        Description = description;
        Parameters = parameters;
        Execute = execute;
    }
    public string FunctionName {  get; set; }
    public string Description { get; set; }
    public List<ChatToolParameter> Parameters { get; set; }
    public Func<List<DevGPTChatMessage>, DevGPTChatToolCall, Task<string>> Execute { get; set; }
}
