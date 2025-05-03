using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.OpnieuwOpnieuw
{
    public class FileStore : BaseStore
    {
        public string StorePath { get; set; }
        public string EmbeddingsFilePath { get; set; }

        public FileStore(string storePath, string embeddingsFilePath, IEmbeddingProvider embeddingProvider) : base(embeddingProvider)
        {
            StorePath = storePath;
            EmbeddingsFilePath = embeddingsFilePath;
        }

        public override void StoreData(string key, string value)
        {
            File.WriteAllText(GetPath(key), value);
        }

        private string GetPath(string key)
        {
            return Path.Combine(StorePath, key);
        }

        public override void RemoveData(string key)
        {
            File.Delete(GetPath(key));
        }
    }
}
