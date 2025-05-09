using System.Collections.Generic;
using System.Threading.Tasks;
using Store.Model;

namespace Store
{
    public interface EmbeddingI
    {
        string Type { get; set; }
        float[] Values { get; set; }
        string Source { get; set; }
    }

    public abstract class AStore
    {
        public abstract Task<List<DocumentInfo>> GetDocumentsAsync();
        public abstract Task<List<EmbeddingsFile>> GetEmbeddingsAsync();
        public abstract Task<DocumentInfo> GetDocumentAsync(string id);
        public abstract Task AddOrUpdateDocumentAsync(DocumentInfo doc);
        public abstract Task RemoveDocumentAsync(string id);
        // Embedding logic
        public abstract Task AddEmbeddingAsync(string docId, Embedding embedding);
    }
}
