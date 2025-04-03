using System.IO;
using System.Security.Cryptography.X509Certificates;
using OpenAI;
using OpenAI.Chat;
using SharpToken;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class SimpleOpenAIClientChatInteraction
{
    public OpenAIClient API { get; set; }
    public ChatClient Client { get; set; }
    public ChatCompletionOptions Options { get; set; }
    public List<ChatMessage> Messages { get; set; }


    public IToolsContext ToolsContext { get; set; }

    public SimpleOpenAIClientChatInteraction(IToolsContext context, OpenAIClient api, string apiKey, string model, ChatClient chatClient, List<ChatMessage> messages, ChatResponseFormat responseFormat, bool useWebSerach, bool useReasoning)
    {
        ToolsContext = context;
        API = api;
        Client = chatClient;
        Options = GetOptions(responseFormat, useWebSerach, useReasoning);
        Messages = messages;
    }

    private ChatCompletionOptions GetOptions(ChatResponseFormat responseFormat, bool useWebSerach, bool useReasoning)
    {
        var options = new ChatCompletionOptions
        {
            ResponseFormat = responseFormat
        };

        if (ToolsContext != null && ToolsContext.Tools != null)
        {
            foreach (var tool in ToolsContext.Tools)
            {
                options.Tools.Add(tool.Definition);
            }
        }

        return options;
    }

    public async Task<ChatCompletion> Run()
    {
        bool requiresAction;
        ChatCompletion completion;
        do
        {
            requiresAction = false;
            completion = await Client.CompleteChatAsync(Messages, Options);

            var finishMessage = new AssistantChatMessage(completion);
            var toolCalls = completion.ToolCalls;
            var finishReason = completion.FinishReason;
            
            requiresAction = await HandleFinishReason(requiresAction, finishMessage, toolCalls, finishReason);
        } while (requiresAction);

        return completion;
    }

    class ToolCallData
    {
        public string FunctionName = "";
        public string ToolCallId = "";
        public List<BinaryData> BinaryData;
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> Stream()
    {
        bool requiresAction;

        string content = "";
        do
        {
            requiresAction = false;

            var completionResult = Client.CompleteChatStreaming(Messages, Options);

            var toolCallData = new List<ToolCallData>();
            ChatFinishReason? finishReason = null;
            var i = 0;
            foreach (StreamingChatCompletionUpdate completionUpdate in completionResult)
            {
                if (completionUpdate.ContentUpdate.Count > 0)
                {
                    content += completionUpdate.ContentUpdate[0].Text;
                    // @later stream chunk
                }

                if (completionUpdate.ToolCallUpdates.Count > 0)
                {
                    foreach(var toolUpdate in completionUpdate.ToolCallUpdates)
                    {
                        if (!toolCallData.Any())
                        {
                            toolCallData.Add(new ToolCallData { BinaryData = new List<BinaryData>() });
                        }

                        var id = completionUpdate.ToolCallUpdates[0].ToolCallId;
                        if (id != null)
                        {
                            if(toolCallData[i].BinaryData.Any())
                            {
                                toolCallData.Add(new ToolCallData { BinaryData = new List<BinaryData>() });
                                ++i;
                            }
                            toolCallData[i].ToolCallId += id;
                        }
                        var fn = completionUpdate.ToolCallUpdates[0].FunctionName;
                        if (fn != null)
                            toolCallData[i].FunctionName += fn;
                        var binary = completionUpdate.ToolCallUpdates[0].FunctionArgumentsUpdate;
                        if(binary != null)
                            toolCallData[i].BinaryData.Add(binary);
                    }
                }

                finishReason = completionUpdate.FinishReason;
                if (finishReason != null)
                    break;

                yield return completionUpdate;
            }
            var toolCalls = toolCallData.Select(d => ChatToolCall.CreateFunctionToolCall(d.ToolCallId, d.FunctionName, ConcatenateArguments(d.BinaryData)));

            var finishMessage = toolCalls.Any() ? new AssistantChatMessage(toolCalls) : null;
            requiresAction = await HandleFinishReason(false, finishMessage, toolCalls, finishReason.Value);
        } while (requiresAction);
    }

    private static BinaryData ConcatenateArguments(List<BinaryData> arguments)
    {
        using (var memoryStream = new MemoryStream())
        {
            foreach (var arg in arguments)
            {
                var data = arg.ToArray();
                memoryStream.Write(data, 0, data.Length);
            }
            return BinaryData.FromBytes(memoryStream.ToArray());
        }
    }

    private async Task<bool> HandleFinishReason(bool requiresAction, AssistantChatMessage finishMessage, IEnumerable<ChatToolCall> toolCalls, ChatFinishReason finishReason)
    {
        switch (finishReason)
        {
            case ChatFinishReason.Stop:
                {
                    break;
                }

            case ChatFinishReason.ToolCalls:
                {
                    await HandleToolCalls(Messages, toolCalls, finishMessage);
                    requiresAction = true;
                    break;
                }

            case ChatFinishReason.Length:
                throw new NotImplementedException("Incomplete model output due to MaxTokens parameter or token limit exceeded.");

            case ChatFinishReason.ContentFilter:
                throw new NotImplementedException("Omitted content due to a content filter flag.");

            case ChatFinishReason.FunctionCall:
                throw new NotImplementedException("Deprecated in favor of tool calls.");

            default:
                throw new NotImplementedException(finishReason.ToString());
        }

        return requiresAction;
    }

    private async Task<List<ChatMessage>> HandleToolCalls(List<ChatMessage> messages, IEnumerable<ChatToolCall> toolCalls, AssistantChatMessage toolCallsMessage)
    {
        var toolResults = new List<ChatMessage>() { toolCallsMessage };
        // Then, add a new tool message for each tool call that is resolved.
        foreach (ChatToolCall toolCall in toolCalls)
        {
            foreach(var tool in ToolsContext.Tools)
            {
                if (toolCall.FunctionName == tool.FunctionName)
                {
                    string result = await tool.Execute(messages, toolCall);
                    toolResults.Add(new ToolChatMessage(toolCall.Id, result));
                }
            }
        }
        messages.AddRange(toolResults);            
        return messages;
    }
}
