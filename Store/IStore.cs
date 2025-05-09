using Store.Model;
using System.Collections.Generic;

namespace Store
{
    public interface IStore
    {
        EmbeddingsFile CreateEmbeddings(IEnumerable<string> documents);
        IEnumerable<string> FindRelevantDocuments(string query, EmbeddingsFile embeddingsFile, int topN);
    }
}
