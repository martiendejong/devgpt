using DevGPT.NewAPI;

namespace Store.OpnieuwOpnieuw.DocumentStore
{
    public class DocumentStore : IDocumentStore
    {
        public IEmbeddingStore EmbeddingStore { get; set; }
        public IDocumentPartStore PartStore { get; set; }
        public DocumentSplitter DocumentSplitter = new DocumentSplitter();
        public DocumentStore(IEmbeddingStore embeddingStore, IDocumentPartStore partStore) 
        {
            EmbeddingStore = embeddingStore;
            PartStore = partStore;
        }

        public void Store(string name, string content)
        {
            var parts = DocumentSplitter.SplitDocument(content);
            var partKeys = new List<string>();
            if (parts.Count == 1)
            {
                EmbeddingStore.Store(name, content);
                partKeys.Add(name);
            }
            else
            {
                for(var i = 0; i < parts.Count; ++i)
                {
                    var partKey = $"{name} part {i}";
                    EmbeddingStore.Store(partKey, parts[i]);
                    partKeys.Add(partKey);
                }
            }
            PartStore.Store(name, partKeys);
        }

        public void Remove(string name)
        {
            EmbeddingStore.Remove(name);
        }
    }
}
