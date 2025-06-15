using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class EmbeddingFileStore : AbstractTextEmbeddingStore, ITextEmbeddingStore
{
    public string EmbeddingsFilePath { get; set; }

    public override EmbeddingInfo[] Embeddings
    {
        get
        {
            return _embeddings.ToArray();
        }
    }

    public List<EmbeddingInfo> _embeddings;

    // Constructor for full functionality (with embedding provider)
    public EmbeddingFileStore(string embeddingsFilePath, ILLMClient embeddingProvider) : base(embeddingProvider)
    {
        EmbeddingsFilePath = embeddingsFilePath;
        _embeddings = ReadEmbeddingsFile();
    }

    // Constructor for read-only scenarios or where embedding is not needed
    public EmbeddingFileStore(string embeddingsFilePath) : base(null)
    {
        EmbeddingsFilePath = embeddingsFilePath;
        _embeddings = ReadEmbeddingsFile();
    }

    public List<EmbeddingInfo> ReadEmbeddingsFile()
    {
        if (File.Exists(EmbeddingsFilePath))
        {
            try
            {
                var data = File.ReadAllText(EmbeddingsFilePath);
                var e = JsonSerializer.Deserialize<List<EmbeddingInfo>>(data);
                if (e != null)
                    return e;
            }
            catch { }
        }
        return new List<EmbeddingInfo>();
    }

    public void LoadEmbeddingsFile()
    {
        _embeddings = ReadEmbeddingsFile();
    }

    public async Task StoreEmbeddingsFile()
    {
        await File.WriteAllTextAsync(EmbeddingsFilePath, JsonSerializer.Serialize(Embeddings));
    }

    public override async Task<EmbeddingInfo?> GetEmbedding(string key)
    {
        return _embeddings.FirstOrDefault(e => e.Key == key);
    }

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
