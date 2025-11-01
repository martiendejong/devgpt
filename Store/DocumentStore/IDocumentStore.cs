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
    public string GetPath(string id);
    public Task<string> Get(string id);

    /// <summary>
    /// Adds an item to the document store under the given key/name
    /// </summary>
    /// <param name="id">The key under which</param>
    /// <param name="content"></param>
    /// <param name="split"></param>
    /// <returns></returns>
    public Task<bool> Store(string id, string content, bool split = true);
    public Task<bool> Embed(string key);
    public Task<bool> Remove(string id);
    public Task<bool> Move(string name, string newName, bool split = true);
    public Task<List<TreeNode<string>>> Tree();
    public Task<List<string>> List(string folder = "", bool recursive = false);
    Task UpdateEmbeddings();
    Task<List<string>> RelevantItems(string query);
    Task<List<RelevantEmbedding>> Embeddings(string relevancyQuery);
}
