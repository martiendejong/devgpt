using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevGPT.NewAPI;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Store.OpnieuwOpnieuw.DocumentStore
{
    public interface IDocumentStore
    {
        public IEmbeddingStore EmbeddingStore { get; }
        public Task Store(string name, string content);
        public void Remove(string name);
        List<TreeNode<IEnumerable<string>>> Tree();
        List<string> List();
    }
}
