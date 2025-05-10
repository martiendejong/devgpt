using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

public interface IDocumentStore
{
    public string Name { get; set; }
    public ITextEmbeddingStore EmbeddingStore { get; }
    public ITextStore TextStore { get; set; }
    public string GetPath(string name);
    public Task<string> Get(string name);
    public Task<bool> Store(string name, string content, bool split = true);
    public Task<bool> Embed(string name);
    public Task<bool> Remove(string name);
    public Task<List<TreeNode<string>>> Tree();
    public Task<List<string>> List();
    Task UpdateEmbeddings();
    Task<List<string>> RelevantItems(string query);
    Task<List<RelevantEmbedding>> Embeddings(string relevancyQuery);
}