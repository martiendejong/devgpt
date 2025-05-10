using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace JavaAnalyze
{
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
            var storeConfig = new DocumentStoreConfig(@"c:\stores\webservice", @"c:\stores\webservice\embeddings", openAiApiKey);
            var store = new DocumentStore(storeConfig);

            // Use PathProvider from Store.Helpers
            var pathProvider = new Store.Helpers.PathProvider(path);

            // Use standard logic to find Java files
            string[] filters = new[] { "*.java" };
            var foundFiles = new List<string>();
            foreach (var filter in filters)
            {
                // PathProvider only resolves base path and relative path; for search, use Directory. Use helpers if available later.
                foundFiles.AddRange(System.IO.Directory.GetFiles(path, filter, System.IO.SearchOption.AllDirectories));
            }

            Console.WriteLine($"Found {foundFiles.Count} Java files:");
            foreach (var f in foundFiles)
                Console.WriteLine(f);

            // Index the Java files in the document store with embeddings (use shared logic)
            foreach (var filePath in foundFiles)
            {
                var relPath = System.IO.Path.GetRelativePath(path, filePath);
                await store.AddDocument(filePath, relPath, relPath, split: true);
            }

            // Update and persist embeddings
            await store.UpdateEmbeddings();
            store.SaveEmbeddings();
        }
    }
}
