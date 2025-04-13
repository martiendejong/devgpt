using DevGPT.NewAPI;

var path = @"C:\projects\BRCWebservice\2025-02-05\Workspace_Gen3";

var openAISettings = OpenAISettings.Load();
string openAiApiKey = openAISettings.ApiKey;

var appFolderStoreConfig = new DocumentStoreConfig(@"c:\stores\webservice", @"c:\stores\webservice\embeddings", openAiApiKey);
var store = new DocumentStore(appFolderStoreConfig);


List<string> javaFiles = new List<string>();

try
{
    javaFiles.AddRange(Directory.GetFiles(path, "*.java", SearchOption.AllDirectories));

    Console.WriteLine($"Found {javaFiles.Count} Java files:");
    foreach (string filePath in javaFiles)
    {
        Console.WriteLine(filePath);
    }
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




await store.UpdateEmbeddings();
store.SaveEmbeddings();


