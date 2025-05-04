using DevGPT.NewAPI;
using Store.OpnieuwOpnieuw.AIClient;
using Store.OpnieuwOpnieuw.DocumentStore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Store.OpnieuwOpnieuw
{
    public class EmbeddingBaseStore : BaseStore<string>
    {
        public EmbeddingInfo[] Embeddings => _embeddings.ToArray();
        public ILLMClient EmbeddingProvider { get; set; }
        public List<EmbeddingInfo> _embeddings = new();

        // Events
        public event EventHandler<StoreUpdateEventArgs<string>> BeforeUpdate;
        public event EventHandler<StoreUpdateEventArgs<string>> AfterUpdate;
        public event EventHandler<StoreRemoveEventArgs> BeforeRemove;
        public event EventHandler<StoreRemoveEventArgs> AfterRemove;

        public EmbeddingBaseStore(ILLMClient embeddingProvider)
        {
            EmbeddingProvider = embeddingProvider;
        }

        override public void Store(string key, string value)
        {
            BeforeUpdate?.Invoke(this, new StoreUpdateEventArgs<string>(key, value));
            StoreEmbedding(key, value);
            AfterUpdate?.Invoke(this, new StoreUpdateEventArgs<string>(key, value));
        }

        override public bool Remove(string key)
        {
            BeforeRemove?.Invoke(this, new StoreRemoveEventArgs(key));
            var result = RemoveEmbedding(key);
            AfterRemove?.Invoke(this, new StoreRemoveEventArgs(key));
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
