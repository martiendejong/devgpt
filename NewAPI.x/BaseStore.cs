using System.IO;

namespace DevGPT.NewAPI
{
    public abstract class BaseStore : AStore, IStore
    {
        public BaseStore(StoreConfig config)
            : base(config)
        {
        }

        public async Task<bool> UpdateEmbedding(string name, string path)
        {
            var absNewPath = PathProvider.GetPath(path);
            var checksum = CalculateChecksum(absNewPath);

            var embedding = Embeddings.FirstOrDefault(e => e.Path == path);

            if (embedding != null)
            {
                if (embedding.Checksum == checksum)
                {
                    return true;
                }
                Embeddings.Remove(embedding);
            }

            var data = await FetchEmbeddingData(absNewPath);
            embedding = new Embedding(name, path, checksum, new EmbeddingData(data));
            Embeddings.Add(embedding);

            return true;
        }

        public async Task<bool> RemoveDocument(string path)
        {
            var embedding = Embeddings.FirstOrDefault(e => e.Path == path);
            if (embedding == null) return false;

            Embeddings.Remove(embedding);
            File.Delete(PathProvider.GetPath(path));

            return true;
        }

        public async Task<bool> AddDocument(string absOrgPath, string name, string relPath = "")
        {
            string absNewPath;
            if (PathProvider.IsRelative)
            {
                absNewPath = PathProvider.GetPath(relPath);
                File.Copy(absOrgPath, absNewPath);
            }
            else
            {
                absNewPath = relPath = absOrgPath;
            }

            return await UpdateEmbedding(name, relPath);
        }

        public async Task<bool> ModifyDocument(string name, string path, string contents)
        {
            File.WriteAllText(PathProvider.GetPath(path), contents);
            await UpdateEmbedding(name, path);

            return true;
        }
    }
}