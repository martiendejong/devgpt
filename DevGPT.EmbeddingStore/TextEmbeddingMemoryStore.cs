public class TextEmbeddingMemoryStore : AbstractTextEmbeddingStore, ITextEmbeddingStore
{
    public TextEmbeddingMemoryStore(ILLMClient embeddingProvider) : base(embeddingProvider) { }

    public override EmbeddingInfo[] Embeddings
    {
        get
        {
            return _embeddings.ToArray();
        }
    }

    public List<EmbeddingInfo> _embeddings = new List<EmbeddingInfo>();

    protected override async Task UpdateEmbedding(EmbeddingInfo embedding) { }
    protected override async Task AddEmbedding(EmbeddingInfo embeddingInfo)
    {
        _embeddings.Add(embeddingInfo);
    }

    public override async Task<EmbeddingInfo?> GetEmbedding(string key)
    {
        return _embeddings.FirstOrDefault(e => e.Key == key);
    }

    public override async Task<bool> RemoveEmbedding(string key)
    {
        var embedding = _embeddings.FirstOrDefault(e => e.Key == key);
        if (embedding == null) return false;
        _embeddings.Remove(embedding);
        return true;
    }
}
