using DevGPT.NewAPI;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using OpenAI.Images;

namespace Store.OpnieuwOpnieuw.AIClient
{
    public class OpenAIClientWrapper : ILLMClient
    {
        public OpenAIConfig Config { get; set; }
        private readonly EmbeddingClient EmbeddingClient;
        private readonly OpenAIClient API; // todo rename
        private OpenAIStreamHandler StreamHandler { get; set; }

        // todo in config
        public string Model { get; set; } = "gpt-4.1";//"gpt-4o";
        public string ImageModel { get; set; } = "gpt-image-1";//"dall-e-3";

        public OpenAIClientWrapper(OpenAIConfig config)
        {
            Config = config;
            EmbeddingClient = new EmbeddingClient("text-embedding-ada-002", config.ApiKey);
            API = new OpenAIClient(config.ApiKey);
            StreamHandler = new OpenAIStreamHandler();
        }

        public async Task<Embedding> GenerateEmbedding(string text)
        {
            var response = await EmbeddingClient.GenerateEmbeddingAsync(text);
            var embeddings = response.Value.ToFloats().ToArray().Select(f => (double)f);
            return new Embedding(embeddings);
        }

        public async Task<string> GetResponseStream(
            List<DevGPTChatMessage> messages,
            Action<string> onChunkReceived, DevGPTChatResponseFormat responseFormat, IToolsContext toolsContext, List<ImageData> images)
        {
            return await StreamHandler.HandleStream(onChunkReceived, StreamChatResult(messages.OpenAI(), responseFormat.OpenAI(), toolsContext, images));
        }

        public async Task<string> GetResponse(
            List<DevGPTChatMessage> messages, DevGPTChatResponseFormat responseFormat, IToolsContext toolsContext, List<ImageData> images)
        {
            return GetText(await GetChatResult(messages.OpenAI(), responseFormat.OpenAI(), toolsContext, images));
        }

        public async Task<DevGPTGeneratedImage> GetImage(
            string prompt, DevGPTChatResponseFormat responseFormat, IToolsContext toolsContext, List<ImageData> images)
        {
            return (await GetImageResult(prompt, responseFormat.OpenAI(), toolsContext, images)).DevGPT();
        }

        #region internal

        protected async Task<ChatCompletion> GetChatResult(List<ChatMessage> messages, ChatResponseFormat responseFormat, IToolsContext context, List<ImageData> images)
        {
            var client = API.GetChatClient(Model);
            var imageClient = API.GetImageClient(Model);
            var interaction = new SimpleOpenAIClientChatInteraction(context, API, Config.ApiKey, Model, client, imageClient, messages, images, responseFormat, true, true);
            return await interaction.Run();
        }

        protected async Task<GeneratedImage> GetImageResult(string prompt, ChatResponseFormat responseFormat, IToolsContext context, List<ImageData> images)
        {
            var client = API.GetChatClient(Model);
            var imageClient = API.GetImageClient(ImageModel);
            var interaction = new SimpleOpenAIClientChatInteraction(context, API, Config.ApiKey, Model, client, imageClient, [prompt], images, responseFormat, true, true);
            return await interaction.RunImage(prompt);
        }

        private IAsyncEnumerable<StreamingChatCompletionUpdate> StreamChatResult(List<ChatMessage> messages, ChatResponseFormat responseFormat, IToolsContext context, List<ImageData> images)
        {
            var client = API.GetChatClient(Model);
            var imageClient = API.GetImageClient(Model);
            var interaction = new SimpleOpenAIClientChatInteraction(context, API, Config.ApiKey, Model, client, imageClient, messages, images, responseFormat, true, true);
            return interaction.Stream();
        }

        protected string GetText(ChatCompletion result)
            => result.Content.ToList().First().Text;

        #endregion
    }
}
