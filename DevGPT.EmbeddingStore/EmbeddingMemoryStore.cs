using DevGPT.NewAPI;
using Store.OpnieuwOpnieuw.AIClient;
using Store.OpnieuwOpnieuw.DocumentStore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Store.OpnieuwOpnieuw
{
    public class EmbeddingMemoryStore : AbstractStore<string>, IEmbeddingStore
    {
        public EmbeddingInfo[] Embeddings => _embeddings.ToArray();
        public ILLMClient EmbeddingProvider { get; set; }
        public List<EmbeddingInfo> _embeddings = new();

        public EmbeddingMemoryStore(ILLMClient embeddingProvider)
        {
            EmbeddingProvider = embeddingProvider;
        }

        override public async Task Store(string key, string value)
        {
            InvokeBeforeUpdate(key, value);
            await StoreEmbedding(key, value);
            InvokeAfterUpdate(key, value);
        }

        override public bool Remove(string key)
        {
            InvokeBeforeRemove(key);
            var result = RemoveEmbedding(key);
            InvokeAfterRemove(key);
            return result;
        }

        public async Task<bool> StoreEmbedding(string key, string value)
        {
            var embedding = _embeddings.FirstOrDefault(e => e.Key == key);
            var checksum = Checksum.CalculateChecksumFromString(value);
            if (embedding == null)
            {
                var embeddingData = await EmbeddingProvider.GenerateEmbedding($"key:\n{key}\nvalue:\n{value}");
                _embeddings.Add(new EmbeddingInfo(key, embeddingData, checksum));
            }
            else if (checksum == embedding.Checksum)
            {
                return false;
            }
            else
            {
                var embeddingData = await EmbeddingProvider.GenerateEmbedding($"key:\n{key}\nvalue:\n{value}");
                embedding.Checksum = checksum;
                embedding.Data = embeddingData;
            }
            return true;
        }

        public bool RemoveEmbedding(string key)
        {
            var embedding = _embeddings.FirstOrDefault(e => e.Key == key);
            if (embedding == null) return false;
            _embeddings.Remove(embedding);
            return true;
        }
    }
}
