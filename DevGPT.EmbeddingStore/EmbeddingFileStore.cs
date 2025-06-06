using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class EmbeddingFileStore : AbstractTextEmbeddingStore, ITextEmbeddingStore
{
    public string EmbeddingsFilePath { get; set; }

    public override EmbeddingInfo[] Embeddings => _embeddings.ToArray();
    public List<EmbeddingInfo> _embeddings;

    // Constructor for full functionality (with embedding provider)
    public EmbeddingFileStore(string embeddingsFilePath, ILLMClient embeddingProvider) : base(embeddingProvider)
    {
        EmbeddingsFilePath = embeddingsFilePath;
        LoadEmbeddingsFile();
    }

    // Constructor for read-only scenarios or where embedding is not needed
    public EmbeddingFileStore(string embeddingsFilePath) : base(null)
    {
        EmbeddingsFilePath = embeddingsFilePath;
        LoadEmbeddingsFile();
    }

    public void LoadEmbeddingsFile()
    {
        if (File.Exists(EmbeddingsFilePath))
        {
            try
            {
                var data = File.ReadAllText(EmbeddingsFilePath);
                _embeddings = JsonSerializer.Deserialize<List<EmbeddingInfo>>(data);
                return;
            }
            catch { }
        }            
        _embeddings = new List<EmbeddingInfo>();
    }

    public async Task StoreEmbeddingsFile()
    {
        await File.WriteAllTextAsync(EmbeddingsFilePath, JsonSerializer.Serialize(Embeddings));
    }

    public override async Task<EmbeddingInfo> GetEmbedding(string key) => _embeddings.FirstOrDefault(e => e.Key == key);

    public override async Task<bool> RemoveEmbedding(string key)
    {
        var embedding = await GetEmbedding(key);
        if (embedding == null) return false;
        _embeddings.Remove(embedding);
        await StoreEmbeddingsFile();
        return true;
    }

    protected override async Task UpdateEmbedding(EmbeddingInfo embedding) {
        await StoreEmbeddingsFile();
    }

    protected override async Task AddEmbedding(EmbeddingInfo embeddingInfo)
    {
        _embeddings.Add(embeddingInfo);
        await StoreEmbeddingsFile();
    }
}
