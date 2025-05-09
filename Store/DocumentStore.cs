using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevGPT.Helpers.Embedding;
using DevGPT.Helpers.FileTree;
using DevGPT.Helpers;
using Store.Model;
using Store.Helpers;

namespace Store
{
    public class DocumentStore : AStore
    {
        private readonly List<DocumentInfo> _documents = new();
        private readonly List<EmbeddingsFile> _embeddings = new();
        private readonly RelevantDocumentsProvider _relevantProvider;
        public DocumentStore()
        {
            _relevantProvider = new RelevantDocumentsProvider(_embeddings);
        }

        public override async Task<List<DocumentInfo>> GetDocumentsAsync()
        {
            // Demo: just return local cache
            await Task.Yield();
            return _documents.ToList();
        }
        public override async Task<List<EmbeddingsFile>> GetEmbeddingsAsync()
        {
            await Task.Yield();
            return _embeddings.ToList();
        }
        public override async Task<DocumentInfo> GetDocumentAsync(string id)
        {
            await Task.Yield();
            return _documents.FirstOrDefault(d => d.Id == id);
        }
        public override async Task AddOrUpdateDocumentAsync(DocumentInfo doc)
        {
            await Task.Yield();
            var idx = _documents.FindIndex(d => d.Id == doc.Id);
            if (idx == -1)
                _documents.Add(doc);
            else
                _documents[idx] = doc;
        }
        public override async Task RemoveDocumentAsync(string id)
        {
            await Task.Yield();
            _documents.RemoveAll(d => d.Id == id);
            _embeddings.RemoveAll(e => e.DocumentId == id);
        }
        public override async Task AddEmbeddingAsync(string docId, Embedding embedding)
        {
            await Task.Yield();
            var found = _embeddings.FirstOrDefault(e => e.DocumentId == docId);
            if (found == null)
            {
                found = new EmbeddingsFile { DocumentId = docId };
                _embeddings.Add(found);
            }
            found.Embeddings.Add(embedding);
        }

        public IEnumerable<EmbeddingsFile> GetRelevantFiles(string query, int max = 5)
            => _relevantProvider.GetRelevant(query, max);
    }
}
