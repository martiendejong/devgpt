using System.Text.Json;
using DevGPT.NewAPI;

namespace Store.OpnieuwOpnieuw.DocumentStore
{
    public class DocumentPartFileStore : IDocumentPartStore
    {
        public string PartsFilePath { get; set; }
        public DocumentPartFileStore(string partsFilePath) { PartsFilePath = partsFilePath;
            LoadPartsFile();
        }

        private void LoadPartsFile()
        {
            if (File.Exists(PartsFilePath))
            {
                try
                {
                    var data = File.ReadAllText(PartsFilePath);
                    Parts = JsonSerializer.Deserialize<Dictionary<string, IEnumerable<string>>>(data);
                    return;
                }
                catch { }
            }
            Parts = new Dictionary<string, IEnumerable<string>>();
        }

        public void StorePartsFile()
        {
            var data = JsonSerializer.Serialize(Parts);
            File.WriteAllText(PartsFilePath, data);
        }

        public Dictionary<string, IEnumerable<string>> Parts;

        public async Task<bool> Store(string name, IEnumerable<string> partKeys)
        {
            Parts[name] = partKeys.ToArray();
            StorePartsFile();
            return true;
        }

        public async Task<IEnumerable<string>> Get(string name)
        {
            return Parts[name];
        }

        public async Task<bool> Remove(string name, IEnumerable<string> partKeys)
        {
            Parts.Remove(name);
            StorePartsFile();
            return true;
        }
    }
}
