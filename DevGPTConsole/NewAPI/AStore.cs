using System.IO;
using System;
using System.Security.Cryptography;

namespace DevGPT.NewAPI
{
    public abstract class AStore
    {
        public List<Embedding> Embeddings { get; set; }

        protected StoreConfig Config { get; set; }
        private EmbeddingsFile EmbeddingsFile { get; set; }
        public EmbeddingGenerator EmbeddingGenerator { get; set; }
        public RelevantDocumentsProvider RelevantDocumentsProvider { get; set; }
        protected PathProvider PathProvider { get; set; }

        public AStore(StoreConfig config)
        {
            Config = config;

            PathProvider = new PathProvider(config.Path);
            EmbeddingsFile = new EmbeddingsFile(config.EmbeddingsFile);
            EmbeddingGenerator = new EmbeddingGenerator(config.OpenAiApiKey);
            RelevantDocumentsProvider = new RelevantDocumentsProvider(EmbeddingGenerator, PathProvider);

            if (File.Exists(EmbeddingsFile.Path))
                LoadEmbeddings();
            else
                Embeddings = new List<Embedding>();
        }

        public void SaveEmbeddings()
        {
            EmbeddingsFile.Save(Embeddings);
        }

        public void LoadEmbeddings()
        {
            Embeddings = EmbeddingsFile.Load();
        }

        protected async Task<EmbeddingData> FetchEmbeddingData(string name, string path, string fullPath)
        {
            var text = File.ReadAllText(fullPath);
            var data = $"PATH: {path}\nDOCUMENT: {name}\n{text}";
            return await EmbeddingGenerator.FetchEmbedding(text);
        }

        protected string CalculateChecksum(string filePath)
        {
            if (!File.Exists(filePath))
                return "";
            using (var sha256 = SHA256.Create())
            using (var fileStream = File.OpenRead(filePath))
            {
                var hashBytes = sha256.ComputeHash(fileStream);
                return Convert.ToHexString(hashBytes); // Converts to a readable hex string
            }
        }
    }
}