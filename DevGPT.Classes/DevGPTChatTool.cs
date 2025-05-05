public class DevGPTChatTool
{
    public DevGPTChatTool(string name, string description, List<ChatToolParameter> parameters)
    {
        Name = name;
        Description = description;
        Parameters = parameters;
    }

    public string Name { get; }
    public string Description { get; }
    public List<ChatToolParameter> Parameters { get; }
}