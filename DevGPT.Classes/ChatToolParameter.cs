using System.Text.Json;

public class ChatToolParameter
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public bool Required { get; set; }
    
    public bool TryGetValue(DevGPTChatToolCall call, out string value)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);
        if(argumentsJson.RootElement.TryGetProperty(Name, out JsonElement element))
        {
            value = element.ToString();
            return true;
        }
        value = null;
        return false;
    }
}
