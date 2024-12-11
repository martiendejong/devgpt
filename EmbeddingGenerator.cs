using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Embedding;
using DevGPT;
using System.Runtime.CompilerServices;

public class EmbeddingGenerator 
{ 
    private readonly string openaiApiKey; 
    private readonly EmbeddingHandler embeddingHandler; 
    public EmbeddingGenerator(string apiKey) 
    { 
        openaiApiKey = apiKey; 
        embeddingHandler = new EmbeddingHandler(apiKey); 
    } 
    public async Task<bool> GenerateAndStoreEmbeddings(string folderPath, string embeddingsFile) 
    {
        Dictionary<string, List<double>> embeddings;
        if (File.Exists(embeddingsFile))
        {
            var existingEmbeddings = JsonConvert.DeserializeObject<Dictionary<string, List<double>>>(await File.ReadAllTextAsync(embeddingsFile));
            var documentContents = GetChangedFilesContents(folderPath);
            var newEmbeddings = await embeddingHandler.GenerateEmbeddings(documentContents);
            foreach (var key in newEmbeddings.Keys)
            {
                existingEmbeddings[key.Replace("/", "\\")] = newEmbeddings[key];
            }
            embeddings = existingEmbeddings;
        }
        else
        {
            var documentContents = await GetAllFilesContents(folderPath);
            embeddings = await embeddingHandler.GenerateEmbeddings(documentContents);
        }


        var json = JsonConvert.SerializeObject(embeddings, Formatting.Indented); 
        await File.WriteAllTextAsync(embeddingsFile, json); 
        Console.WriteLine($"Embeddings saved to {embeddingsFile}");

        return true;
    }

    private async Task<Dictionary<string, string>> GetAllFilesContents(string folderPath)
    {
        var documents = new Dictionary<string, string>();

        ProjectLoader loader = new ProjectLoader();
        var allFiles = loader.GetFiles(folderPath);
        foreach (var file in allFiles)
        {
            var relativePath = Path.GetRelativePath(folderPath, file);

            if (File.Exists(file))
            {
                documents[relativePath] = await File.ReadAllTextAsync(file);
            }
        }

        return documents;
    }

    private Dictionary<string, string> GetChangedFilesContents(string folderPath) 
    { 
        var result = new Dictionary<string, string>(); 
        var processStartInfo = new System.Diagnostics.ProcessStartInfo("git", "diff --name-only HEAD") 
        { 
            RedirectStandardOutput = true, 
            WorkingDirectory = folderPath, 
            UseShellExecute = false, 
            CreateNoWindow = true 
        }; 
        using (var process = System.Diagnostics.Process.Start(processStartInfo)) 
        { 
            if (process != null) 
            { 
                using (var reader = process.StandardOutput) 
                { 
                    while (!reader.EndOfStream) 
                    { 
                        var relativePath = reader.ReadLine(); 
                        if (File.Exists(Path.Combine(folderPath, relativePath))) 
                        { 
                            result[relativePath] = File.ReadAllText(Path.Combine(folderPath, relativePath)); 
                        } 
                    } 
                } 
            } 
        } 
        return result; 
    } 
}