#nullable enable

public class DevGPTChatMessage
{
    public DevGPTMessageRole Role { get; set; }
    public string Text { get; set; }
    
    public DevGPTChatMessage()
    {
        Role = DevGPTMessageRole.User;
        Text = string.Empty;
    }
    public DevGPTChatMessage(DevGPTMessageRole role, string text)
    {
        Role = role;
        Text = text;
    }
}
