public interface ITextEmbeddingStore /*: IDictionary<string, string>*/
{
    public Task<bool> StoreEmbedding(string key, string value);
    public Task<EmbeddingInfo?> GetEmbedding(string key);
    public Task<bool> RemoveEmbedding(string key);
    //public Task<string> GetValue(string key);
    public EmbeddingInfo[] Embeddings { get; }
}
