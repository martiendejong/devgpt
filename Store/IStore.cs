using OpenAI.Chat;

namespace DevGPT.NewAPI
{
    public interface IStore
    {
        

        void SaveEmbeddings();
        void LoadEmbeddings();

        Task<bool> AddDocument(string absPath, string name, string path, bool split);
        Task<bool> RemoveDocument(string path);
        Task<bool> ModifyDocument(string path, string name, string contents);
        Task<bool> UpdateEmbedding(string path, string name);

        Task<List<ChatMessage>> GetRelevantDocumentsAsChatMessages(string query);
        Task<string> GetRelevantDocumentsAsString(string query);
        Task<List<string>> GetRelevantDocuments(string query);
        string GetFilesList();
    }
}