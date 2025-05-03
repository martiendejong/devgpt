using DevGPT.NewAPI;

namespace Store.OpnieuwOpnieuw
{
    public interface IEmbeddingProvider
    {
        public Embedding GetEmbeddingData(string data);
    }
}
