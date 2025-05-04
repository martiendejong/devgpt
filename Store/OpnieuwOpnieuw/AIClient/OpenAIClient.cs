using DevGPT.NewAPI;
using OpenAI.Embeddings;

namespace Store.OpnieuwOpnieuw.AIClient
{
    public class OpenAIClient : ILLMClient
    {
        public OpenAIConfig Config { get; set; }
        private readonly EmbeddingClient Client;
        public OpenAIClient(OpenAIConfig config)
        {
            Config = config;
            Client = new EmbeddingClient("text-embedding-ada-002", config.ApiKey);
        }

        public async Task<Embedding> GenerateEmbedding(string text)
        {
            var response = await Client.GenerateEmbeddingAsync(text);
            var embeddings = response.Value.ToFloats().ToArray().Select(f => (double)f);
            return new Embedding(embeddings);
        }
    }
}
