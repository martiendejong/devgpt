namespace Store.OpnieuwOpnieuw
{
    public interface IEmbeddingStore : IDictionary<string, string>
    {
        public Task Store(string key, string value);
        public EmbeddingInfo[] Embeddings { get; }
    }
}
