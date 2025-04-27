using System.ClientModel;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Images;
using static System.Net.Mime.MediaTypeNames;
using static TypedOpenAIClient;

public class SimpleOpenAIClient
{
    public string ApiKey { get; set; }
    public OpenAIClient API { get; set; }
    public string Model { get; set; } = "gpt-4.1";//"gpt-4o";
    public string ImageModel { get; set; } = "gpt-image-1";//"dall-e-3";

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
        Action<string> onChunkReceived, ChatResponseFormat responseFormat, IToolsContext toolsContext, List<ImageData> images)
    {
        return ChainLog(messages, await StreamHandler.HandleStream(onChunkReceived, StreamChatResult(messages, responseFormat, toolsContext, images)));
    }

    public async Task<string> GetResponse(
        List<ChatMessage> messages, ChatResponseFormat responseFormat, IToolsContext toolsContext, List<ImageData> images)
    {
        return ChainLog(messages, GetText(await GetChatResult(messages, responseFormat, toolsContext, images)));
    }

    public async Task<GeneratedImage> GetImage(
        string prompt, ChatResponseFormat responseFormat, IToolsContext toolsContext, List<ImageData> images)
    {
        return await GetImageResult(prompt, responseFormat, toolsContext, images);
    }

    #region internal

    private StreamHandler StreamHandler { get; set; }
    protected async Task<ChatCompletion> GetChatResult(List<ChatMessage> messages, ChatResponseFormat responseFormat, IToolsContext context, List<ImageData> images)
    {
        var client = API.GetChatClient(Model);
        var imageClient = API.GetImageClient(Model);
        var interaction = new SimpleOpenAIClientChatInteraction(context, API, ApiKey, Model, client, imageClient, messages, images, responseFormat, true, true);
        return await interaction.Run();
    }

    protected async Task<GeneratedImage> GetImageResult(string prompt, ChatResponseFormat responseFormat, IToolsContext context, List<ImageData> images)
    {
        var client = API.GetChatClient(Model);
        var imageClient = API.GetImageClient(ImageModel);
        var interaction = new SimpleOpenAIClientChatInteraction(context, API, ApiKey, Model, client, imageClient, [prompt], images, responseFormat, true, true);
        return await interaction.RunImage(prompt);
    }

    private IAsyncEnumerable<StreamingChatCompletionUpdate> StreamChatResult(List<ChatMessage> messages, ChatResponseFormat responseFormat, IToolsContext context, List<ImageData> images)
    {
        var client = API.GetChatClient(Model);
        var imageClient = API.GetImageClient(Model);
        var interaction = new SimpleOpenAIClientChatInteraction(context, API, ApiKey, Model, client, imageClient, messages, images, responseFormat, true, true);
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
