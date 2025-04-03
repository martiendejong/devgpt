using OpenAI;
using OpenAI.Embeddings;



namespace DevGPT.NewAPI
{
    public class EmbeddingGenerator
    {
        private readonly EmbeddingClient Client;
        public EmbeddingGenerator(string apiKey)
        {
            Client = new EmbeddingClient("text-embedding-ada-002", apiKey);
        }

        public async Task<EmbeddingData> FetchEmbedding(string text)
        {
            try
            {
                var response = await Client.GenerateEmbeddingAsync(text);
                var embeddings = response.Value.ToFloats().ToArray().Select(f => (double)f);
                return new EmbeddingData(embeddings);
            }
            catch (Exception ex)
            {
                Console.WriteLine(@$"Failed to generate embedding for {text}");
                Console.WriteLine(@$"Failed to generate embedding for {ex.Message}");
                throw;
            }
        }
    }
}