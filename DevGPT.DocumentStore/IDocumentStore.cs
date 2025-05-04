using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Store.OpnieuwOpnieuw.DocumentStore
{
    public interface IDocumentStore
    {
        public IEmbeddingStore EmbeddingStore { get; }
        public void Store(string name, string content);
        public void Remove(string name);
    }
}
