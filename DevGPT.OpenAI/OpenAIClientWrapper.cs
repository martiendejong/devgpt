using System.ClientModel;
using System.Text.Json;
using System.Threading.Channels;

using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using OpenAI.Images;
using static System.Net.Mime.MediaTypeNames;

public partial class OpenAIClientWrapper : ILLMClient
{
    public string GetFormatInstruction<ResponseType>() where ResponseType : ChatResponse<ResponseType>, new()
    {
        return $"YOUR OUTPUT WILL ALWAYS BE ONLY A JSON RESPONSE IN THIS FORMAT AND NOTHING ELSE: {ChatResponse<ResponseType>.Signature} EXAMPLE: {JsonSerializer.Serialize(ChatResponse<ResponseType>.Example)}";
    }

    public List<DevGPTChatMessage> AddFormattingInstruction<ResponseType>(List<DevGPTChatMessage> messages) where ResponseType : ChatResponse<ResponseType>, new()
    {
        var formatInstruction = GetFormatInstruction<ResponseType>();
        messages.Insert(messages.Count - 1, new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = formatInstruction });
        return messages;
    }

    public PartialJsonParser Parser { get; set; } = new PartialJsonParser();

    public async Task<ResponseType?> GetResponse<ResponseType>(List<DevGPTChatMessage> messages, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel = default) where ResponseType : ChatResponse<ResponseType>, new()
    {
        return Parser.Parse<ResponseType>(await GetResponse(AddFormattingInstruction<ResponseType>(messages), DevGPTChatResponseFormat.Json, toolsContext, images, cancel));
    }

    public async Task<ResponseType?> GetResponseStream<ResponseType>(List<DevGPTChatMessage> messages, Action<string> onChunkReceived, IToolsContext? toolsContext, List<ImageData>? images) where ResponseType : ChatResponse<ResponseType>, new()
    {
        return Parser.Parse<ResponseType>(await GetResponseStream(AddFormattingInstruction<ResponseType>(messages), onChunkReceived, DevGPTChatResponseFormat.Json, toolsContext, images));
    }
}

public partial class OpenAIClientWrapper : ILLMClient
{
    public OpenAIConfig Config { get; set; }
    private readonly EmbeddingClient EmbeddingClient;
    private readonly OpenAIClient API;
    private OpenAIStreamHandler StreamHandler { get; set; }

    public OpenAIClientWrapper(OpenAIConfig config)
    {
        Config = config;
        EmbeddingClient = new EmbeddingClient(config.EmbeddingModel, config.ApiKey);
        API = new OpenAIClient(config.ApiKey);
        StreamHandler = new OpenAIStreamHandler();
    }

    public async Task<Embedding> GenerateEmbedding(string text)
    {
        return await Retry.Run(async () =>
        {
            var response = await EmbeddingClient.GenerateEmbeddingAsync(text);
            var embeddings = response.Value.ToFloats().ToArray().Select(f => (double)f);
            return new Embedding(embeddings);
        });
    }

    public async Task<string> GetResponseStream(
        List<DevGPTChatMessage> messages,
        Action<string> onChunkReceived, DevGPTChatResponseFormat responseFormat, IToolsContext? toolsContext, List<ImageData>? images)
    {
        return await StreamHandler.HandleStream(onChunkReceived, StreamChatResult(messages.OpenAI(), responseFormat.OpenAI(), toolsContext, images));
    }

    public async Task<string> GetResponse(
        List<DevGPTChatMessage> messages, DevGPTChatResponseFormat responseFormat, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel = default)
    {
        return GetText(await GetChatResult(messages.OpenAI(), responseFormat.OpenAI(), toolsContext, images, cancel));
    }

    public async Task<DevGPTGeneratedImage> GetImage(
        string prompt, DevGPTChatResponseFormat responseFormat, IToolsContext? toolsContext, List<ImageData>? images)
    {
        return (await GetImageResult(prompt, responseFormat.OpenAI(), toolsContext, images)).DevGPT();
    }

    #region internal

    protected async Task<ChatCompletion> GetChatResult(List<ChatMessage> messages, ChatResponseFormat responseFormat, IToolsContext? context, List<ImageData>? images, CancellationToken cancel = default)
    {
        var client = API.GetChatClient(Config.Model);
        var imageClient = API.GetImageClient(Config.Model);
        var interaction = new SimpleOpenAIClientChatInteraction(context, API, Config.ApiKey, Config.Model, client, imageClient, messages, images, responseFormat, true, true);
        return await interaction.Run(cancel);
    }

    protected async Task<GeneratedImage> GetImageResult(string prompt, ChatResponseFormat responseFormat, IToolsContext? context, List<ImageData>? images)
    {
        var client = API.GetChatClient(Config.Model);
        var imageClient = API.GetImageClient(Config.ImageModel);
        var interaction = new SimpleOpenAIClientChatInteraction(context, API, Config.ApiKey, Config.Model, client, imageClient, [prompt], images, responseFormat, true, true);
        return await interaction.RunImage(prompt);
    }

    private IAsyncEnumerable<StreamingChatCompletionUpdate> StreamChatResult(List<ChatMessage> messages, ChatResponseFormat responseFormat, IToolsContext? context, List<ImageData>? images)
    {
        var client = API.GetChatClient(Config.Model);
        var imageClient = API.GetImageClient(Config.Model);
        var interaction = new SimpleOpenAIClientChatInteraction(context, API, Config.ApiKey, Config.Model, client, imageClient, messages, images, responseFormat, true, true);
        return interaction.Stream();
    }

    protected string GetText(ChatCompletion result)
    {
        return result.Content.ToList().First().Text;
    }

    #endregion
}
