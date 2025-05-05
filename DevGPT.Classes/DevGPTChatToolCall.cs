
public class DevGPTChatToolCall
{
    private string id;
    private string functionName;
    private BinaryData functionArguments;

    public DevGPTChatToolCall(string id, string functionName, BinaryData functionArguments)
    {
        this.id = id;
        this.functionName = functionName;
        this.functionArguments = functionArguments;
    }
}