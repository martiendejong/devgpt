namespace DevGPT.NewAPI
{
    public class DocumentStoreConfig
    {
        public string Path { get; set; }

        public string EmbeddingsFile { get; set; }
        public string OpenAiApiKey { get; set; }

        public DocumentStoreConfig(string path, string embeddingsFile, string openAiApiKey)
        {
            Path = path;
            EmbeddingsFile = embeddingsFile;
            OpenAiApiKey = openAiApiKey;
        }
    }
}