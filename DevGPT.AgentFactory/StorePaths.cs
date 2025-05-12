// See https://aka.ms/new-console-template for more information
public class StorePaths
{
    public StorePaths(string rootFolder, string embeddingsFile = "", string partsFile = "") 
    {
        Directory.CreateDirectory(rootFolder);
        if (string.IsNullOrWhiteSpace(embeddingsFile)) embeddingsFile = Path.Combine(rootFolder, "embeddings");
        if(string.IsNullOrWhiteSpace(partsFile)) partsFile = Path.Combine(rootFolder, "parts");
        RootFolder = rootFolder;
        EmbeddingsFile = embeddingsFile;
        PartsFile = partsFile;
    }
    public string RootFolder;
    public string EmbeddingsFile;
    public string PartsFile;
}
