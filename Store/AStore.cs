using System.IO;
using System;

namespace DevGPT.NewAPI
{
    public abstract class AStore
    {        
        protected List<Embedding> Embeddings { get; set; }
        protected DocumentStoreConfig Config { get; set; }
        protected IObjectListFile<Embedding> EmbeddingsFile { get; set; }
        protected EmbeddingGenerator EmbeddingGenerator { get; set; }
        public RelevantDocumentsProvider RelevantDocumentsProvider { get; protected set; }
        protected PathProvider PathProvider { get; set; }

        public AStore(DocumentStoreConfig config)
        {
            Config = config;

            PathProvider = new PathProvider(config.Path);
            EmbeddingsFile = new EmbeddingsFile(config.EmbeddingsFile);
            EmbeddingGenerator = new EmbeddingGenerator(config.OpenAiApiKey);
            RelevantDocumentsProvider = new RelevantDocumentsProvider(EmbeddingGenerator, PathProvider);

            if (EmbeddingsFile.Exists)
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

        public List<Embedding> GetEmbeddings()
        {
            return Embeddings;
        }
    }
}