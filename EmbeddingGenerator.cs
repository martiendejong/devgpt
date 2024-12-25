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

    public async Task<bool> UpdateEmbeddings(string folderPath, string embeddingsFile, string[] files)
    {
        var documentContents = GetFilesContents(folderPath, files);
        var existingEmbeddings = await LoadEmbeddings(embeddingsFile);
        var newEmbeddings = await embeddingHandler.GenerateEmbeddings(documentContents);
        AddEmbeddings(existingEmbeddings, newEmbeddings);
        var embeddings = existingEmbeddings;

        await SaveEmbeddings(embeddingsFile, embeddings);
        
        return true;
    }

    private Dictionary<string, string> GetFilesContents(string folderPath, string[] files)
    {
        var result = new Dictionary<string, string>();
        foreach (var file in files)
        {
            result[file] = File.ReadAllText(Path.Combine(folderPath, file));
        }
        return result;
    }

    public async Task<bool> GenerateAndStoreEmbeddings(string folderPath, string embeddingsFile)
    {
        Dictionary<string, List<double>> embeddings;
        if (File.Exists(embeddingsFile))
        {
            var documentContents = GetChangedFilesContents(folderPath);
            var existingEmbeddings = await LoadEmbeddings(embeddingsFile);
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

        await SaveEmbeddings(embeddingsFile, embeddings);

        return true;
    }

    private static void AddEmbeddings(Dictionary<string, List<double>> existingEmbeddings, Dictionary<string, List<double>> newEmbeddings)
    {
        foreach (var key in newEmbeddings.Keys)
        {
            existingEmbeddings[key] = newEmbeddings[key];
        }
    }

    private static async Task<Dictionary<string, List<double>>> LoadEmbeddings(string embeddingsFile)
    {
        return JsonConvert.DeserializeObject<Dictionary<string, List<double>>>(await File.ReadAllTextAsync(embeddingsFile));
    }

    private static async System.Threading.Tasks.Task SaveEmbeddings(string embeddingsFile, Dictionary<string, List<double>> embeddings)
    {
        var json = JsonConvert.SerializeObject(embeddings, Formatting.Indented);
        await File.WriteAllTextAsync(embeddingsFile, json);
        Console.WriteLine($"Embeddings saved to {embeddingsFile}");
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

    private Dictionary<string, string> GetChangedAndAddedFilesContents(string folderPath)
    {
        var result = new Dictionary<string, string>();

        // Command to get modified and added files
        var gitDiffCmd = new System.Diagnostics.ProcessStartInfo("git", "diff --name-status HEAD")
        {
            RedirectStandardOutput = true,
            WorkingDirectory = folderPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Command to get untracked (newly added) files
        var gitUntrackedCmd = new System.Diagnostics.ProcessStartInfo("git", "ls-files --others --exclude-standard")
        {
            RedirectStandardOutput = true,
            WorkingDirectory = folderPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Process for modified and added files (tracked)
        using (var process = System.Diagnostics.Process.Start(gitDiffCmd))
        {
            if (process != null)
            {
                using (var reader = process.StandardOutput)
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var split = line.Split('\t');
                        var status = split[0]; // A for added, M for modified
                        var relativePath = split[1];

                        var fullPath = Path.Combine(folderPath, relativePath);
                        if ((status == "A" || status == "M") && File.Exists(fullPath))
                        {
                            result[relativePath] = File.ReadAllText(fullPath);
                        }
                    }
                }
            }
        }

        // Process for untracked files (newly added but unversioned)
        using (var process = System.Diagnostics.Process.Start(gitUntrackedCmd))
        {
            if (process != null)
            {
                using (var reader = process.StandardOutput)
                {
                    while (!reader.EndOfStream)
                    {
                        var relativePath = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(relativePath)) continue;

                        var fullPath = Path.Combine(folderPath, relativePath);
                        if (File.Exists(fullPath))
                        {
                            result[relativePath] = File.ReadAllText(fullPath);
                        }
                    }
                }
            }
        }

        return result;
    }
}