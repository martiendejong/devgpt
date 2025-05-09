using DevGPT.NewAPI;
using Store.OpnieuwOpnieuw.DocumentStore;
using Store.OpnieuwOpnieuw;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using DevGPT.NewAPI; // For PathProvider

class Program
{
    static async Task Main(string[] args)
    {
        // Settings
        var path = @"C:\projects\BRCWebservice\2025-02-05\Workspace_Gen3";
        var openAISettings = OpenAISettings.Load();
        string openAiApiKey = openAISettings.ApiKey;

        // Setup document store config for embeddings and document indexing
        var appFolderStoreConfig = new DocumentStoreConfig(@"c:\stores\webservice", @"c:\stores\webservice\embeddings", openAiApiKey);
        // Modern DocumentStore from DevGPT.Store (for compatibility with AppBuilder)
        var store = new DocumentStore(appFolderStoreConfig);

        // Use shared PathProvider logic from the library
        var pathProvider = new PathProvider(path);

        // Generic shared file search logic
        string[] filters = new[] { "*.java" };
        var foundFiles = new List<string>();
        foreach (var filter in filters)
        {
            try
            {
                // Instead of direct Directory.GetFiles, offer shared logic here if more complex (refactor as needed)
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

        // Add Java files to the store (embed/process them as in AppBuilder)
        foreach (var filePath in foundFiles)
        {
            var relPath = filePath.Substring(path.EndsWith("\\") ? path.Length : (path.Length + 1));
            await store.AddDocument(filePath, relPath, relPath, split: true);
        }

        // Update embeddings for all files in the store, and persist
        await store.UpdateEmbeddings();
        store.SaveEmbeddings();
    }
}
