using System.Text.Json;

namespace Store.OpnieuwOpnieuw.DocumentStore
{
    public class DocumentPartFileStore : DocumentPartMemoryStore
    {
        public string PartsFilePath { get; set; }
        public DocumentPartFileStore(string partsFilePath) { PartsFilePath = partsFilePath;
            AfterUpdate += DocumentPartFileStore_AfterUpdate;
            AfterRemove += DocumentPartFileStore_AfterRemove;
            LoadPartsFile();
        }

        private void LoadPartsFile()
        {
            var data = File.ReadAllText(PartsFilePath);
            Data = JsonSerializer.Deserialize<Dictionary<string, IEnumerable<string>>>(data);
        }

        public void StorePartsFile()
        {
            var data = JsonSerializer.Serialize(Data);
            File.WriteAllText(PartsFilePath, data);
        }

        private void DocumentPartFileStore_AfterRemove(object? sender, StoreRemoveEventArgs e)
        {
            StorePartsFile();
        }

        private void DocumentPartFileStore_AfterUpdate(object? sender, StoreUpdateEventArgs<IEnumerable<string>> e)
        {
            StorePartsFile();
        }
    }
}
