public class AppBuilderConfig
{
    public string ProjectName { get; set; }
    public string FolderPath { get; set; }
    public string EmbeddingsFile { get; set; }
    public string HistoryFile { get; set; }
    public string Query { get; set; }
    public bool GenerateEmbeddings { get; set; }
    public bool UseHistory { get; set; }
}