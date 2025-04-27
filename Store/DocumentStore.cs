using OpenAI.Chat;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;
using static System.Formats.Asn1.AsnWriter;

namespace DevGPT.NewAPI
{
    public class DocumentStore : AStore, IStore
    {
        protected DocumentSplitter DocumentSplitter { get; set; }
        protected Checksum Checksum { get; set; }

        public DocumentStore(DocumentStoreConfig config)
            : base(config)
        {
            Checksum = new Checksum();
            DocumentSplitter = new DocumentSplitter();
        }

        public async Task<bool> UpdateEmbeddings()
        {
            var e = new List<Embedding>(Embeddings);
            foreach (var embedding in e)
            {
                await UpdateEmbedding(embedding.Name, embedding.Path);
            }
            return true;
        }

        public string GetFilePath(string path)
        {
            return PathProvider.GetPath(path);
        }

        public async Task<bool> UpdateEmbedding(string name, string path)
        {
            var absNewPath = PathProvider.GetPath(path);
            var checksum = Checksum.CalculateChecksum(absNewPath);
            var embedding = Embeddings.FirstOrDefault(e => e.Path == path);

            if (embedding != null)
            {
                if (embedding.Checksum == checksum)
                {
                    return true;
                }
                Embeddings.Remove(embedding);
            }

            //if (checksum == "")
            //{
            //    return true;
            //}
            if (File.Exists(absNewPath))
            {
                var data = await FetchEmbeddingData(name, path, absNewPath);
                embedding = new Embedding(name, path, checksum, new EmbeddingData(data));
                Embeddings.Add(embedding);
            }
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

        public List<DocumentInfo> GetFilesAsDocumentInfo()
        {
            return Embeddings.Select(e => new DocumentInfo { Name = e.Name, Path = e.Path }).ToList();
        }

        public List<FileNode> GetFilesTree()
        {
            var nodes = new List<FileNode>();
            var parentNodes = new List<FileNode>();
            Embeddings.ForEach(embedding =>
            {
                var names = embedding.Path.Split(new char[] { '/', '\\' });
                if (names.Length > 1)
                {
                    var node = nodes.SingleOrDefault(n => n.Name == names[0] && n.Parent == null);
                    if (node == null)
                    {
                        node = new FileNode(names[0]);
                        nodes.Add(node);
                        parentNodes.Add(node);
                    }
                    for (var i = 1; i < names.Length - 1; ++i)
                    {
                        var newNode = nodes.SingleOrDefault(n => n.Name == names[0] && n.Parent == node);
                        if (newNode == null)
                        {
                            newNode = new FileNode(names[0]) { Parent = node };
                            node.Children.Add(newNode);
                            nodes.Add(newNode);
                            node = newNode;
                        }
                    }
                    var document = new FileNode(embedding.Name) { Parent = node, Path = embedding.Path };
                    node.Children.Add(document);
                }
                else
                {
                    var document = new FileNode(embedding.Name) { Parent = null, Path = embedding.Path };
                    parentNodes.Add(document);
                }
            });

            return parentNodes;
        }

        public async Task<bool> AddWholeDocument(string absOrgPath, string name, string relPath = "")
        {
            string absNewPath;
            if (PathProvider.IsRelative)
            {
                absNewPath = PathProvider.GetPath(relPath);
                if (absOrgPath != absNewPath)
                    File.Copy(absOrgPath, absNewPath);
            }
            else
            {
                absNewPath = relPath = absOrgPath;
            }

            return await UpdateEmbedding(name, relPath);
        }

        public async Task<bool> AddDocument(string pathToDocument, string name, string path = "", bool split = true)
        {
            var storePath = PathProvider.IsRelative ? path : pathToDocument;
            if (split)
                return await SplitAndAddDocument(pathToDocument, name, storePath);
            return await AddWholeDocument(pathToDocument, name, storePath);
        }

        public async Task<bool> ModifyDocument(string name, string path, string contents)
        {
            WriteFile(PathProvider.GetPath(path), contents);
            await UpdateEmbedding(name, path);

            return true;
        }

        public async Task<List<string>> GetRelevantDocuments(string query, List<IStore> otherStores)
        {
            return await RelevantDocumentsProvider.GetRelevantDocuments(query, Embeddings, otherStores);
        }

        public async Task<string> GetRelevantDocumentsAsString(string query, List<IStore> otherStores)
        {
            return await RelevantDocumentsProvider.GetRelevantDocumentsAsString(query, Embeddings, otherStores);
        }

        public async Task<List<ChatMessage>> GetRelevantDocumentsAsChatMessages(string query, List<IStore> otherStores)
        {
            return await RelevantDocumentsProvider.GetRelevantDocumentsAsChatMessages(query, Embeddings, otherStores);
        }

        protected async Task<bool> SplitAndAddDocument(string pathToDocument, string name, string path = "")
        {
            var documents = DocumentSplitter.SplitDocument(pathToDocument);
            await AddDocumentParts(name, path, documents);

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

        protected async Task AddDocumentParts(string name, string relPath, List<string> documents)
        {
            if(documents.Count == 1)
            {
                await ModifyDocument(name, relPath, documents[0]);
                return;
            }

            var relPathPeriodIndex = relPath.IndexOf(".");
            var relPathNoExtension = relPath.Substring(0, relPathPeriodIndex);
            var relPathExtension = relPath.Substring(relPathPeriodIndex + 1);
            for (var i = 0; i < documents.Count; ++i)
            {
                var document = documents[i];
                var partPath = $"{relPathNoExtension}.{i}.{relPathExtension}";
                var partName = $"{name} part {i}";
                await ModifyDocument(partName, partPath, document);
            }
        }

        protected async Task<EmbeddingData> FetchEmbeddingData(string name, string path, string fullPath)
        {
            DocumentSplitter.TokensPerPart = 8000;
            var texts = DocumentSplitter.SplitDocument(fullPath);
            DocumentSplitter.TokensPerPart = 100;
            var text = texts.First();
            var data = $"PATH: {path}\nDOCUMENT: {name}\n{text}"; // should this be a setting?
            return await EmbeddingGenerator.FetchEmbedding(data);
        }

        public string GetFilesList()
        {
            return string.Join("\n", Embeddings.Select(e => $"{e.Path}"));
        }
    }
}