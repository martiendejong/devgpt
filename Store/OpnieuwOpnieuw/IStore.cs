namespace Store.OpnieuwOpnieuw
{
    public interface IStore
    {
        public void Store(string key, string value);
        public void Remove(string key);
        public EmbeddingInfo[] Embeddings { get; }
    }
}
