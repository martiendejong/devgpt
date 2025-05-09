using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Store;
using Store.Helpers; // For PathProvider

class Program
{
    static async Task Main(string[] args)
    {
        // Path to the Java project
        string path = @"C:\projects\BRCWebservice\2025-02-05\Workspace_Gen3";

        // Load OpenAI API settings via DevGPT config
        var openAISettings = Store.OpenAISettings.Load();
        string openAiApiKey = openAISettings.ApiKey;

        // Set up DevGPT DocumentStore
        var storeConfig = new Store.Model.DocumentStoreConfig(@"c:\stores\webservice", @"c:\stores\webservice\embeddings", openAiApiKey);
        var store = new Store.DocumentStore(storeConfig);

        // Use PathProvider from DevGPT.Helpers
        var pathProvider = new PathProvider(path);

        // Use shared logic to find Java files
        string[] filters = new[] { "*.java" };
        var foundFiles = new List<string>();
        foreach (var filter in filters)
        {
            try
            {
                foundFiles.AddRange(Directory.GetFiles(path, filter, SearchOption.AllDirectories));
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access denied: {ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine($"Directory not found: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        Console.WriteLine($"Found {foundFiles.Count} Java files:");
        foreach (var f in foundFiles)
            Console.WriteLine(f);

        // Index the Java files in the document store with embeddings
        foreach (var filePath in foundFiles)
        {
            var relPath = filePath.Substring(path.EndsWith("\\") ? path.Length : (path.Length + 1));
            await store.AddDocument(filePath, relPath, relPath, split: true);
        }

        // Update and persist embeddings
        await store.UpdateEmbeddings();
        store.SaveEmbeddings();
    }
}
