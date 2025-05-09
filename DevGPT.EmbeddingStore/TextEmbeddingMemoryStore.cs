using Store.OpnieuwOpnieuw.AIClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Store.OpnieuwOpnieuw
{
    public class TextEmbeddingMemoryStore : AbstractTextEmbeddingStore, ITextEmbeddingStore
    {
        public TextEmbeddingMemoryStore(ILLMClient embeddingProvider) : base(embeddingProvider) { }

        public override EmbeddingInfo[] Embeddings => _embeddings.ToArray();
        public List<EmbeddingInfo> _embeddings = new();

        protected override async Task UpdateEmbedding(EmbeddingInfo embedding) { }
        protected override async Task AddEmbedding(EmbeddingInfo embeddingInfo) => _embeddings.Add(embeddingInfo);
        public override async Task<EmbeddingInfo> GetEmbedding(string key) => _embeddings.FirstOrDefault(e => e.Key == key);
        public override async Task<bool> RemoveEmbedding(string key)
        {
            var embedding = _embeddings.FirstOrDefault(e => e.Key == key);
            if (embedding == null) return false;
            _embeddings.Remove(embedding);
            return true;
        }
    }
}
