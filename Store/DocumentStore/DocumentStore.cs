using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

public class DocumentStore : IDocumentStore
{
    public string Name { get; set; } = Guid.NewGuid().ToString();

    public EmbeddingMatcher EmbeddingMatcher = new EmbeddingMatcher();
    public ITextEmbeddingStore EmbeddingStore { get; set; }
    public IDocumentPartStore PartStore { get; set; }
    public DocumentSplitter DocumentSplitter = new DocumentSplitter();
    public ITextStore TextStore { get; set; }
    public ILLMClient LLMClient { get; set; }
    public IDocumentMetadataStore MetadataStore { get; set; }
    public BinaryDocumentProcessor BinaryProcessor { get; set; }

    public DocumentStore(ITextEmbeddingStore embeddingStore, ITextStore textStore, IDocumentPartStore partStore, IDocumentMetadataStore metadataStore, ILLMClient llmClient)
    {
        LLMClient = llmClient;
        EmbeddingStore = embeddingStore;
        TextStore = textStore;
        PartStore = partStore;
        MetadataStore = metadataStore;
        BinaryProcessor = new BinaryDocumentProcessor(llmClient);
    }


    public async Task UpdateEmbeddings()
    {
        foreach (var embedding in EmbeddingStore.Embeddings) await Embed(embedding.Key);
    }

    public string Sanitize(string name)
    {
        return name;
    }

    public async Task<bool> Embed(string name)
    {
        name = Sanitize(name);
        List<string> partKeys = [name];
        var content = await TextStore.Get(name);
        if (content == null)
        {
            await EmbeddingStore.RemoveEmbedding(name);
            return false;
        }
        var embed = EmbeddingMatcher.CutOffQuery(content);
        return await EmbeddingStore.StoreEmbedding(name, embed);
    }

    public async Task<bool> Store(string name, string content, Dictionary<string, string>? metadata = null, bool split = true)
    {
        name = Sanitize(name);

        // Store metadata
        var docMetadata = new DocumentMetadata
        {
            Id = name,
            OriginalPath = "",
            MimeType = "text/plain",
            Size = content.Length,
            Created = DateTime.UtcNow,
            CustomMetadata = metadata ?? new Dictionary<string, string>(),
            IsBinary = false,
            Summary = null
        };
        await MetadataStore.Store(name, docMetadata);

        var partKeys = new List<string>();

        // First, store metadata as a searchable chunk
        var metadataKey = $"{name}::metadata";
        var metadataChunk = docMetadata.ToChunkText();
        await EmbeddingStore.StoreEmbedding(metadataKey, metadataChunk);
        await TextStore.Store(metadataKey, metadataChunk);
        partKeys.Add(metadataKey);

        // Then store content chunks
        var parts = split ? DocumentSplitter.SplitDocument(content) : [EmbeddingMatcher.CutOffQuery(content)];
        if (parts.Count == 1)
        {
            await EmbeddingStore.StoreEmbedding(name, content);
            await TextStore.Store(name, content);
            partKeys.Add(name);
        }
        else
        {
            for (var i = 0; i < parts.Count; ++i)
            {
                var partKey = $"{name} part {i}";
                await EmbeddingStore.StoreEmbedding(partKey, parts[i]);
                await TextStore.Store(partKey, parts[i]);
                partKeys.Add(partKey);
            }
        }
        await PartStore.Store(name, partKeys);
        return true;
    }

    public async Task<bool> Store(string name, byte[] content, string mimeType, Dictionary<string, string>? metadata = null)
    {
        name = Sanitize(name);

        // Process binary content
        var textContent = await BinaryProcessor.ExtractText(content, mimeType);
        var summary = BinaryProcessor.IsBinary(mimeType) ? await BinaryProcessor.GenerateSummary(content, mimeType) : null;

        // Store metadata
        var docMetadata = new DocumentMetadata
        {
            Id = name,
            OriginalPath = "",
            MimeType = mimeType,
            Size = content.Length,
            Created = DateTime.UtcNow,
            CustomMetadata = metadata ?? new Dictionary<string, string>(),
            IsBinary = BinaryProcessor.IsBinary(mimeType),
            Summary = summary
        };
        await MetadataStore.Store(name, docMetadata);

        var partKeys = new List<string>();

        // Store metadata as a searchable chunk
        var metadataKey = $"{name}::metadata";
        var metadataChunk = docMetadata.ToChunkText();
        await EmbeddingStore.StoreEmbedding(metadataKey, metadataChunk);
        await TextStore.Store(metadataKey, metadataChunk);
        partKeys.Add(metadataKey);

        // Store the extracted/summarized text content
        var contentToStore = string.IsNullOrEmpty(summary) ? textContent : $"{summary}\n\nExtracted content:\n{textContent}";
        var parts = DocumentSplitter.SplitDocument(contentToStore);

        if (parts.Count == 1)
        {
            await EmbeddingStore.StoreEmbedding(name, contentToStore);
            await TextStore.Store(name, contentToStore);
            partKeys.Add(name);
        }
        else
        {
            for (var i = 0; i < parts.Count; ++i)
            {
                var partKey = $"{name} part {i}";
                await EmbeddingStore.StoreEmbedding(partKey, parts[i]);
                await TextStore.Store(partKey, parts[i]);
                partKeys.Add(partKey);
            }
        }

        await PartStore.Store(name, partKeys);
        return true;
    }

    public async Task<bool> StoreFromFile(string name, string filePath, Dictionary<string, string>? metadata = null)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        name = Sanitize(name);

        // Detect MIME type
        var mimeType = BinaryProcessor.DetectMimeType(filePath);
        var fileInfo = new FileInfo(filePath);

        // Read file content
        var content = await File.ReadAllBytesAsync(filePath);

        // Update metadata with file info
        var updatedMetadata = metadata ?? new Dictionary<string, string>();
        updatedMetadata["OriginalPath"] = filePath;
        updatedMetadata["FileName"] = fileInfo.Name;
        updatedMetadata["FileExtension"] = fileInfo.Extension;

        // Determine if binary or text
        if (BinaryProcessor.IsBinary(mimeType))
        {
            return await Store(name, content, mimeType, updatedMetadata);
        }
        else
        {
            var textContent = await File.ReadAllTextAsync(filePath);
            return await Store(name, textContent, updatedMetadata, true);
        }
    }

    public async Task<DocumentMetadata?> GetMetadata(string id)
    {
        id = Sanitize(id);
        return await MetadataStore.Get(id);
    }

    public async Task<bool> Remove(string name)
    {
        name = Sanitize(name);

        // Remove metadata
        await MetadataStore.Remove(name);

        // Remove all embeddings and text
        await EmbeddingStore.RemoveEmbedding(name);
        await TextStore.Remove(name);

        var parts = await PartStore.Get(name);
        foreach (var part in parts)
        {
            await EmbeddingStore.RemoveEmbedding(part);
            await TextStore.Remove(part);
        }

        return true;
    }

    public async Task<bool> Move(string name, string newName, bool split = true)
    {
        var content = await TextStore.Get(name);
        if (content == null) return false;

        // Get and update metadata
        var metadata = await MetadataStore.Get(name);
        var customMetadata = metadata?.CustomMetadata ?? new Dictionary<string, string>();

        await Store(newName, content, customMetadata, split);
        await Remove(name);
        return true;
    }

    public async Task<List<TreeNode<string>>> Tree()
    {
        var names = await PartStore.ListNames();
        return TreeMaker.GetTree(names.Select(n => n).ToList());
    }

    public async Task<List<string>> List(string folder = "", bool recursive = false)
    {
        var names = (await PartStore.ListNames()).ToList();
        if (string.IsNullOrWhiteSpace(folder))
        {
            if (recursive) return names;
            return names.Where(p => !p.Contains('/') && !p.Contains('\\')).ToList();
        }
        // Normalize folder separators for matching
        var f1 = folder.Replace('\\','/').TrimEnd('/');
        return names.Where(p => {
            var np = p.Replace('\\','/');
            if (!np.StartsWith(f1)) return false;
            if (recursive) return true;
            if (np.Length <= f1.Length) return false;
            var rest = np.Substring(f1.Length).TrimStart('/');
            return !rest.Contains('/');
        }).ToList();
    }

    public async Task<List<string>> RelevantItems(string query)
    {
        var embed = await LLMClient.GenerateEmbedding(EmbeddingMatcher.CutOffQuery(query));
        var list = EmbeddingMatcher.GetEmbeddingsWithSimilarity(embed, EmbeddingStore.Embeddings);
        var r = list.Select(item => new RelevantEmbedding { Similarity = item.similarity, StoreName = Name, Document = item.document, GetText = async (string a) => await TextStore.Get(a) }).ToList();
        var items = await EmbeddingMatcher.TakeTop(r);
        return items;
    }

    public async Task<List<RelevantEmbedding>> Embeddings(string query)
    {
        var embed = await LLMClient.GenerateEmbedding(EmbeddingMatcher.CutOffQuery(query));
        var list = EmbeddingMatcher.GetEmbeddingsWithSimilarity(embed, EmbeddingStore.Embeddings);
        var r = list.Select(item => new RelevantEmbedding { Similarity = item.similarity, StoreName = Name, Document = item.document, GetText = async (string a) => await TextStore.Get(a) }).ToList();
        return r;
    }

    public string GetPath(string name)
    {
        return TextStore.GetPath(Sanitize(name));
    }

    public async Task<string> Get(string name)
    {
        return await TextStore.Get(Sanitize(name));
    }
}
