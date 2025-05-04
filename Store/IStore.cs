using OpenAI.Chat;

namespace DevGPT.NewAPI
{
    public interface IStore
    {
        public RelevantDocumentsProvider RelevantDocumentsProvider
        {
            get;
        }
        List<EmbeddingI> GetEmbeddings();

        void SaveEmbeddings();
        void LoadEmbeddings();

        Task<bool> AddDocument(string absPath, string name, string path, bool split);
        Task<bool> RemoveDocument(string path);
        Task<bool> ModifyDocument(string path, string name, string contents);
        Task<bool> UpdateEmbedding(string path, string name);

        Task<List<ChatMessage>> GetRelevantDocumentsAsChatMessages(string query, List<IStore> otherStores);
        Task<string> GetRelevantDocumentsAsString(string query, List<IStore> otherStores);
        Task<List<string>> GetRelevantDocuments(string query, List<IStore> otherStores);
        string GetFilesList();
    }
}