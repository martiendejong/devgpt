namespace DevGPT.NewAPI
{
    public class StoreConfig
    {
        public string Path { get; set; }

        public string EmbeddingsFile { get; set; }
        public string OpenAiApiKey { get; set; }

        public StoreConfig(string path, string embeddingsFile, string openAiApiKey)
        {
            Path = path;
            EmbeddingsFile = embeddingsFile;
            OpenAiApiKey = openAiApiKey;
        }
    }
}