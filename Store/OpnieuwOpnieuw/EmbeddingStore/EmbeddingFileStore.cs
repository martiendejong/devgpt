using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Store.OpnieuwOpnieuw.AIClient;

namespace Store.OpnieuwOpnieuw
{
    public class EmbeddingFileStore : EmbeddingBaseStore
    {
        public string StorePath { get; set; }
        public string EmbeddingsFilePath { get; set; }

        public EmbeddingFileStore(string storePath, string embeddingsFilePath, ILLMClient embeddingProvider) : base(embeddingProvider)
        {
            StorePath = storePath;
            EmbeddingsFilePath = embeddingsFilePath;
            AfterUpdate += StoreData;
            AfterRemove += RemoveData;
        }

        private void StoreData(object sender, StoreUpdateEventArgs<string> args)
        {
            File.WriteAllText(GetPath(args.Key), args.Value);
        }

        private string GetPath(string key)
        {
            return Path.Combine(StorePath, key);
        }

        private void RemoveData(object sender, StoreRemoveEventArgs args)
        {
            File.Delete(GetPath(args.Key));
        }
    }
}
