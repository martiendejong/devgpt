using System.Text.Json;

namespace Store.OpnieuwOpnieuw.DocumentStore
{
    public class DocumentPartFileStore : DocumentPartBaseStore
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
            DocumentParts = JsonSerializer.Deserialize<Dictionary<string, string[]>>(data);
        }

        public void StorePartsFile()
        {
            var data = JsonSerializer.Serialize(DocumentParts);
            File.WriteAllText(PartsFilePath, data);
        }

        private void DocumentPartFileStore_AfterRemove(object? sender, StoreRemoveEventArgs e)
        {
            StorePartsFile();
        }

        private void DocumentPartFileStore_AfterUpdate(object? sender, StoreUpdateEventArgs<string[]> e)
        {
            StorePartsFile();
        }
    }
}
