using System.Linq;
using System.Xml.Linq;
using DevGPT.NewAPI;
using Store.OpnieuwOpnieuw.AIClient;
using Store.OpnieuwOpnieuw.Helpers.FileTree;

namespace Store.OpnieuwOpnieuw.DocumentStore
{
    public class DocumentStore : IDocumentStore
    {
        public string Name { get; set; } = Guid.NewGuid().ToString();

        public EmbeddingMatcher EmbeddingMatcher = new EmbeddingMatcher();
        public ITextEmbeddingStore EmbeddingStore { get; set; }
        public IDocumentPartStore PartStore { get; set; }
        public DocumentSplitter DocumentSplitter = new DocumentSplitter();
        public ITextStore TextStore { get; set; }
        public ILLMClient LLMClient { get; set; }
        public DocumentStore(ITextEmbeddingStore embeddingStore, ITextStore textStore, IDocumentPartStore partStore, ILLMClient llmClient)
        {
            LLMClient = llmClient;
            EmbeddingStore = embeddingStore;
            TextStore = textStore;
            PartStore = partStore;
        }


        public async Task UpdateEmbeddings()
        {
            foreach (var embedding in EmbeddingStore.Embeddings) await Embed(embedding.Key);
        }

        public async Task<bool> Embed(string name)
        {
            List<string> partKeys = [name];
            var content = await TextStore.Get(name);
            var embed = EmbeddingMatcher.CutOffQuery(content);
            return await EmbeddingStore.StoreEmbedding(name, embed);
        }

        public async Task<bool> Store(string name, string content, bool split = true)
        {
            var partKeys = new List<string>();
            if (!split)
            {
                var embed = EmbeddingMatcher.CutOffQuery(content);
                await EmbeddingStore.StoreEmbedding(name, embed);
                await TextStore.Store(name, content);
                partKeys.Add(name);
            }
            else
            {
                var parts = DocumentSplitter.SplitDocument(content);
                if (parts.Count == 1)
                {
                    await EmbeddingStore.StoreEmbedding(name, content);
                    partKeys.Add(name);
                }
                else
                {
                    for (var i = 0; i < parts.Count; ++i)
                    {
                        var partKey = $"{name} part {i}";
                        await EmbeddingStore.StoreEmbedding(partKey, parts[i]);
                        await TextStore.Store(partKey, content);
                        partKeys.Add(partKey);
                    }
                }
            }
            await PartStore.Store(name, partKeys);
            return true;
        }

        public async Task<bool> Remove(string name)
        {
            await EmbeddingStore.RemoveEmbedding(name);
            var parts = await PartStore.Get(name);
            foreach (var part in parts)
            {
                await EmbeddingStore.RemoveEmbedding(part);
            }
            return true;
        }

        public async Task<List<TreeNode<string>>> Tree() => TreeMaker.GetTree(EmbeddingStore.Embeddings.Select(e => e.Key).ToList());
        public async Task<List<string>> List() => EmbeddingStore.Embeddings.Select(e => e.Key).ToList();
        public async Task<List<string>> RelevantItems(string query)
        {
            var embed = await LLMClient.GenerateEmbedding(EmbeddingMatcher.CutOffQuery(query));
            var list = EmbeddingMatcher.GetEmbeddingsWithSimilarity(embed, EmbeddingStore.Embeddings);
            var r = list.Select(item => new RelevantEmbedding { Similarity = item.similarity, StoreName = Name, Document = item.document, GetText = async (string a) => await TextStore.Get(a) }).ToList();
            var items = await EmbeddingMatcher.TakeTop(r);
            return items;
        }

        public async Task<List<RelevantEmbedding>> Embeddings(string query)
        {
            var embed = await LLMClient.GenerateEmbedding(EmbeddingMatcher.CutOffQuery(query));
            var list = EmbeddingMatcher.GetEmbeddingsWithSimilarity(embed, EmbeddingStore.Embeddings);
            var r = list.Select(item => new RelevantEmbedding { Similarity = item.similarity, StoreName = Name, Document = item.document, GetText = async (string a) => await TextStore.Get(a) }).ToList();
            return r;
        }

        public string GetPath(string name) => TextStore.GetPath(name);

        public async Task<string> Get(string name) => await TextStore.Get(name);
    }
}
