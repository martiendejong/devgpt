using System;using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Embedding;
using DevGPT;
public class EmbeddingGenerator 
{ 
    private readonly string openaiApiKey; 
    private readonly EmbeddingHandler embeddingHandler; 
    public EmbeddingGenerator(string apiKey) 
    { 
        openaiApiKey = apiKey; 
        embeddingHandler = new EmbeddingHandler(apiKey); 
    } 
    public async Task GenerateAndStoreEmbeddings(string folderPath, string embeddingsFile) 
    { 
        var documentContents = GetChangedFilesContents(folderPath); 
        var embeddings = await embeddingHandler.GenerateEmbeddings(documentContents); 
        var json = JsonConvert.SerializeObject(embeddings, Formatting.Indented); 
        await File.WriteAllTextAsync(embeddingsFile, json); 
        Console.WriteLine($"Embeddings saved to {embeddingsFile}"); 
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