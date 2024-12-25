namespace DevGPT.NewAPI
{
    public interface IStore
    {
        List<Embedding> Embeddings { get; set; }

        void SaveEmbeddings();
        void LoadEmbeddings();

        Task<bool> AddDocument(string absPath, string name, string path);
        Task<bool> RemoveDocument(string path);
        Task<bool> ModifyDocument(string path, string name, string contents);
        Task<bool> UpdateEmbedding(string path, string name);
    }
}