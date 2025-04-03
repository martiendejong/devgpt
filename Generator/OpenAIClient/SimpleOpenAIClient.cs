using System.ClientModel;
using OpenAI;
using OpenAI.Chat;
using static System.Net.Mime.MediaTypeNames;
using static TypedOpenAIClient;

public class SimpleOpenAIClient
{
    public string ApiKey { get; set; }
    public OpenAIClient API { get; set; }
    public string Model { get; set; } = "gpt-4o";

    public delegate void LogFn(List<ChatMessage> messages, string responseContent);
    public LogFn Log { get; set; }

    public SimpleOpenAIClient(OpenAIClient api, string apiKey, LogFn log)
    {
        ApiKey = apiKey;
        API = api;
        StreamHandler = new StreamHandler();
        Log = log;
    }

    public async Task<string> GetResponseStream(
        List<ChatMessage> messages,
        Action<string> onChunkReceived, ChatResponseFormat responseFormat, IToolsContext toolsContext)
    {
        return ChainLog(messages, await StreamHandler.HandleStream(onChunkReceived, StreamChatResult(messages, responseFormat, toolsContext)));
    }

    public async Task<string> GetResponse(
        List<ChatMessage> messages, ChatResponseFormat responseFormat, IToolsContext toolsContext)
    {
        return ChainLog(messages, GetText(await GetChatResult(messages, responseFormat, toolsContext)));
    }

    #region internal

    private StreamHandler StreamHandler { get; set; }
    protected async Task<ChatCompletion> GetChatResult(List<ChatMessage> messages, ChatResponseFormat responseFormat, IToolsContext context)
    {
        var client = API.GetChatClient(Model);
        var interaction = new SimpleOpenAIClientChatInteraction(context, API, ApiKey, Model, client, messages, responseFormat, true, true);
        return await interaction.Run();
    }

    private IAsyncEnumerable<StreamingChatCompletionUpdate> StreamChatResult(List<ChatMessage> messages, ChatResponseFormat responseFormat, IToolsContext context)
    {
        var client = API.GetChatClient(Model);
        var interaction = new SimpleOpenAIClientChatInteraction(context, API, ApiKey, Model, client, messages, responseFormat, true, true);
        return interaction.Stream();
        //var interaction = new SimpleOpenAIClientChatInteraction(client, messages, responseFormat, true, true);
        //return await interaction.Stream();


        //var response = API.GetChatClient(Model).CompleteChatStreamingAsync(messages, GetOptions(responseFormat)); ;


        // Check if a function was called

        //return client.CompleteChatStreamingAsync(messages);//  Chat.StreamChatEnumerableAsync(GetChatRequest(messages, responseFormat));
    }

    protected string GetText(ChatCompletion result)
        => result.Content.ToList().First().Text;

    protected string ChainLog(List<ChatMessage> messages, string content)
    {
        Log(messages, content);
        return content;
    }
    #endregion
}
