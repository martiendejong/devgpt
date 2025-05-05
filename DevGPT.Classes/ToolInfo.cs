using Store.OpnieuwOpnieuw.AIClient;

public class ToolInfo
{
    public ToolInfo(string name, string description, List<ChatToolParameter> parameters, Func<List<DevGPTChatMessage>, DevGPTChatToolCall, Task<string>> execute)
    {
        Name = name;
        Description = description;
        Parameters = parameters;
        Execute = execute;
    }
    public string Name {  get; set; }
    public string Description { get; set; }
    public List<ChatToolParameter> Parameters { get; set; }
    public Func<List<DevGPTChatMessage>, DevGPTChatToolCall, Task<string>> Execute { get; set; }
}
