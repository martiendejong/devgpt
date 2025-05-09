using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevGPT.NewAPI;
using Store.OpnieuwOpnieuw.AIClient;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Store.OpnieuwOpnieuw.DocumentStore
{
    public interface IDocumentStore
    {
        public ITextEmbeddingStore EmbeddingStore { get; }
        public string GetPath(string name);
        public Task<string> Get(string name);
        public Task<bool> Store(string name, string content, bool split = true);
        public Task<bool> Embed(string name);
        public Task<bool> Remove(string name);
        public Task<List<TreeNode<string>>> Tree();
        public Task<List<string>> List();
        Task UpdateEmbeddings();
    }
}
