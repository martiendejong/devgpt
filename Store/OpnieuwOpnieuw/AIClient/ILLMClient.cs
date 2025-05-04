using DevGPT.NewAPI;

namespace Store.OpnieuwOpnieuw.AIClient
{
    public interface ILLMClient
    {
        public Task<Embedding> GenerateEmbedding(string data);
    }
}
