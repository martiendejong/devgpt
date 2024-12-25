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
        protected EmbeddingGenerator EmbeddingGenerator { get; set; }
        protected RelevantDocumentsProvider RelevantDocumentsProvider { get; set; }
        protected PathProvider PathProvider { get; set; }

        public AStore(StoreConfig config)
        {
            Config = config;

            PathProvider = new PathProvider(config.Path);
            EmbeddingsFile = new EmbeddingsFile(PathProvider.GetPath(config.EmbeddingsFile));
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

        protected async Task<EmbeddingData> FetchEmbeddingData(string fullPath)
        {
            var text = File.ReadAllText(fullPath);
            return await EmbeddingGenerator.FetchEmbedding(text);
        }

        protected string CalculateChecksum(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var fileStream = File.OpenRead(filePath))
            {
                var hashBytes = sha256.ComputeHash(fileStream);
                return Convert.ToHexString(hashBytes); // Converts to a readable hex string
            }
        }
    }
}