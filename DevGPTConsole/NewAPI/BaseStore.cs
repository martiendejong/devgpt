using System.IO;
using System.Linq;

namespace DevGPT.NewAPI
{
    public abstract class BaseStore : AStore, IStore
    {
        public BaseStore(StoreConfig config)
            : base(config)
        {
        }

        public async Task<bool> UpdateEmbeddings()
        {
            var e = Embeddings.ToList();
            foreach (var embedding in e)
            {
                await UpdateEmbedding(embedding.Name, embedding.Path);
            }
            return true;
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
                if (checksum == "") 
                    return true;
            }

            var data = await FetchEmbeddingData(name, path, absNewPath);
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
                if(absOrgPath != absNewPath)
                    File.Copy(absOrgPath, absNewPath);
            }
            else
            {
                absNewPath = relPath = absOrgPath;
            }

            return await UpdateEmbedding(name, relPath);
        }

        public async Task<bool> SplitAndAddDocument(string absOrgPath, string name, string relPath = "")
        {
            var tokensPerPart = 1000;
            var partNr = 0;
            var tokenCounter = new TokenCounter();
            var content = File.ReadAllText(absOrgPath);
            var remainingLines = content.Split("\n").ToList();
            //var totalTokens = tokenCounter.CountTokens(content);
            var relPathPeriodIndex = relPath.IndexOf(".");
            var relPathNoExtension = relPath.Substring(0, relPathPeriodIndex);
            var relPathExtension = relPath.Substring(relPathPeriodIndex + 1);

            while (remainingLines.Count > 0)
            {
                var partName = $"{name} part {partNr}";
                var partPath = $"{relPathNoExtension}.{partNr}.{relPathExtension}";

                var partLines = new List<string>();
                bool partComplete = false;
                var moveLineToPart = () => {
                    partLines.Add(remainingLines[0]);
                    remainingLines.RemoveAt(0);
                    var partTokens = tokenCounter.CountTokens(string.Join("\n", partLines));
                    partComplete = partTokens >= tokensPerPart;
                };
                //partLines.Add(remainingLines[0]);
                //remainingLines.RemoveAt(0);
                //var partContent = partLines.Join("\n");
                //var partTokens = tokenCounter.CountTokens(partContent);
                moveLineToPart();
                while (!partComplete && remainingLines.Count > 0)
                {
                    moveLineToPart();
                    //partLines.Add(remainingLines[0]);
                    //remainingLines.RemoveAt(0);
                    //partContent = partLines.Join("\n");
                    //partTokens = tokenCounter.CountTokens(partContent);
                }

                await ModifyDocument(partName, partPath, string.Join("\n", partLines));
                partNr++;
            }
            return true;
        }

        public async Task<bool> ModifyDocument(string name, string path, string contents)
        {
            WriteFile(PathProvider.GetPath(path), contents);
            await UpdateEmbedding(name, path);

            return true;
        }

        protected void WriteFile(string path, string contents)
        {
            var p = new FileInfo(path);
            CreatePath(p.Directory);
            File.WriteAllText(path, contents);
        }

        protected void CreatePath(DirectoryInfo dir)
        {
            if (!dir.Parent.Exists) CreatePath(dir.Parent);
            dir.Create();
        }
    }
}