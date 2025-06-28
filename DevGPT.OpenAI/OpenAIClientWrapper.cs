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

    public async Task<ResponseType?> GetResponseStream<ResponseType>(List<DevGPTChatMessage> messages, Action<string> onChunkReceived, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel) where ResponseType : ChatResponse<ResponseType>, new()
    {
        return Parser.Parse<ResponseType>(await GetResponseStream(AddFormattingInstruction<ResponseType>(messages), onChunkReceived, DevGPTChatResponseFormat.Json, toolsContext, images, cancel));
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
        Action<string> onChunkReceived, DevGPTChatResponseFormat responseFormat, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel)
    {
        Log(messages.Last()?.Text);
        return await StreamHandler.HandleStream(onChunkReceived, StreamChatResult(messages.OpenAI(), responseFormat.OpenAI(), toolsContext, images, cancel));
    }

    public async Task<string> GetResponse(
        List<DevGPTChatMessage> messages, DevGPTChatResponseFormat responseFormat, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel)
    {
        Log(messages.Last()?.Text);
        return GetText(await GetChatResult(messages.OpenAI(), responseFormat.OpenAI(), toolsContext, images, cancel));
    }

    public async Task<DevGPTGeneratedImage> GetImage(
        string prompt, DevGPTChatResponseFormat responseFormat, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel)
    {
        Log(prompt);
        return (await GetImageResult(prompt, responseFormat.OpenAI(), toolsContext, images, cancel)).DevGPT();
    }

    #region internal

    protected async Task<ChatCompletion> GetChatResult(List<ChatMessage> messages, ChatResponseFormat responseFormat, IToolsContext? context, List<ImageData>? images, CancellationToken cancel)
    {
        var client = API.GetChatClient(Config.Model);
        var imageClient = API.GetImageClient(Config.Model);
        var interaction = new SimpleOpenAIClientChatInteraction(context, API, Config.ApiKey, Config.Model, Config.LogPath, client, imageClient, messages, images, responseFormat, true, true);
        return await interaction.Run(cancel);
    }

    protected async Task<GeneratedImage> GetImageResult(string prompt, ChatResponseFormat responseFormat, IToolsContext? context, List<ImageData>? images, CancellationToken cancel)
    {
        var client = API.GetChatClient(Config.Model);
        var imageClient = API.GetImageClient(Config.ImageModel);
        var interaction = new SimpleOpenAIClientChatInteraction(context, API, Config.ApiKey, Config.Model, Config.LogPath, client, imageClient, [prompt], images, responseFormat, true, true);
        return await interaction.RunImage(prompt, cancel);
    }

    private IAsyncEnumerable<StreamingChatCompletionUpdate> StreamChatResult(List<ChatMessage> messages, ChatResponseFormat responseFormat, IToolsContext? context, List<ImageData>? images, CancellationToken cancel)
    {
        var client = API.GetChatClient(Config.Model);
        var imageClient = API.GetImageClient(Config.Model);
        var interaction = new SimpleOpenAIClientChatInteraction(context, API, Config.ApiKey, Config.Model, Config.LogPath, client, imageClient, messages, images, responseFormat, true, true);
        return interaction.Stream(cancel);
    }

    protected string GetText(ChatCompletion result)
    {
        return result.Content.ToList().First().Text;
    }

    #endregion

    public void Log(string? data)
    {
        const int maxRetries = 10;
        const int delayMs = 100;
        string message = $"{DateTime.Now:yy-MM-dd HH:mm:ss}\n{data ?? ""}";

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                using (FileStream stream = new FileStream(Config.LogPath, FileMode.Append, FileAccess.Write, FileShare.None))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine(message);
                    return;
                }
            }
            catch (IOException)
            {
                // File is likely locked by another writer
                Thread.Sleep(delayMs);
            }
        }

        throw new IOException("Could not write to log file after multiple attempts due to it being locked.");
    }
}
