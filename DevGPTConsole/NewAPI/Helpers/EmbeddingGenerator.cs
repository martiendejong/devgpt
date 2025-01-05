using OpenAI_API;
using OpenAI_API.Embedding;



namespace DevGPT.NewAPI
{
    public class EmbeddingGenerator
    {
        private readonly OpenAIAPI OpenAIAPI;
        public EmbeddingGenerator(string apiKey)
        {
            OpenAIAPI = new OpenAIAPI(apiKey);
        }

        public async Task<EmbeddingData> FetchEmbedding(string text)
        {
            var response = await OpenAIAPI.Embeddings.CreateEmbeddingAsync(new EmbeddingRequest
            {
                Input = text,
                Model = "text-embedding-ada-002"
            });
            return new EmbeddingData(response.Data[0].Embedding.Select(e => (double)e));
        }
    }
}