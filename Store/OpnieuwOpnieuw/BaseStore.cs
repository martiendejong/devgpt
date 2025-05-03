using DevGPT.NewAPI;

namespace Store.OpnieuwOpnieuw
{
    public abstract class BaseStore : IStore
    {
        public EmbeddingInfo[] Embeddings => _embeddings.ToArray();

        public virtual void Store(string key, string value)
        {
            if (StoreEmbedding(key, value))
                StoreData(key, value);
        }

        public abstract void StoreData(string key, string value);
        public abstract void RemoveData(string key);

        public IEmbeddingProvider EmbeddingProvider { get; set; }

        public List<EmbeddingInfo> _embeddings;

        public BaseStore(IEmbeddingProvider embeddingProvider)
        {
            EmbeddingProvider = embeddingProvider;
        }

        public bool StoreEmbedding(string key, string value)
        {
            var embedding = _embeddings.FirstOrDefault(e => e.Key == key);
            var checksum = Checksum.CalculateChecksumFromString(value);
            if (embedding == null)
            {
                var embeddingData = EmbeddingProvider.GetEmbeddingData($"key:\n${key}\nvalue:\n{value}");
                _embeddings.Add(new EmbeddingInfo(key, embeddingData, checksum));
            }
            else if (checksum == embedding.Checksum)
            {
                return false;
            }
            else
            {
                var embeddingData = EmbeddingProvider.GetEmbeddingData($"key:\n${key}\nvalue:\n{value}");
                embedding.Checksum = checksum;
                embedding.Data = embeddingData;
            }
            return true;
        }

        public void Remove(string key)
        {
            if (RemoveEmbedding(key))
                RemoveData(key);
        }

        public bool RemoveEmbedding(string key)
        {
            var embedding = _embeddings.First(e => e.Key == key);
            if (embedding == null) return false;
            _embeddings.Remove(embedding);
            return true;
        }
    }
}
