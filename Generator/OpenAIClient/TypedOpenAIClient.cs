using DevGPT.NewAPI;
using OpenAI;
using OpenAI.Chat;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

public class TypedOpenAIClient : SimpleOpenAIClient
{
    public string GetFormatInstruction<ResponseType>() where ResponseType : ChatResponse<ResponseType>, new()
        => $"YOUR OUTPUT WILL ALWAYS BE ONLY A JSON RESPONSE IN THIS FORMAT AND NOTHING ELSE: {ChatResponse<ResponseType>.Signature} EXAMPLE: {ChatResponse<ResponseType>.Example.Serialize()}";
    
    public List<ChatMessage> AddFormattingInstruction<ResponseType>(List<ChatMessage> messages) where ResponseType : ChatResponse<ResponseType>, new()
    {
        var formatInstruction = GetFormatInstruction<ResponseType>();
        messages.Insert(messages.Count - 1, new SystemChatMessage(formatInstruction));
        return messages;
    }

    public PartialJsonParser Parser { get; set; }
    public TypedOpenAIClient(OpenAIClient api, string apiKey, LogFn log) : base(api, apiKey, log)
    {
        Parser = new PartialJsonParser();
    }

    public virtual async Task<ResponseType> GetResponse<ResponseType>(List<ChatMessage> messages, IToolsContext toolsContext) where ResponseType : ChatResponse<ResponseType>, new()
        => Parser.Parse<ResponseType>(await GetResponse(AddFormattingInstruction<ResponseType>(messages), ChatResponseFormat.CreateJsonObjectFormat(), toolsContext));

    public virtual async Task<ResponseType> GetResponseStream<ResponseType>(List<ChatMessage> messages, Action<string> onChunkReceived, IToolsContext toolsContext) where ResponseType : ChatResponse<ResponseType>, new()
        => Parser.Parse<ResponseType>(await GetResponseStream(AddFormattingInstruction<ResponseType>(messages), onChunkReceived, ChatResponseFormat.CreateJsonObjectFormat(), toolsContext));
}