using System.Text.Json;

public class ChatToolParameter
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public bool Required { get; set; }
    
    public bool TryGetValue(DevGPTChatToolCall call, out JsonElement element)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);
        return argumentsJson.RootElement.TryGetProperty("key", out element);
    }
}
