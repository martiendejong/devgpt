using OpenAI.Chat;

public class Tool
{
    public string FunctionName { get; set; }
    public ChatTool Definition { get; set; }
    public Func<List<ChatMessage>, ChatToolCall, Task<string>> Execute { get; set; }
}
