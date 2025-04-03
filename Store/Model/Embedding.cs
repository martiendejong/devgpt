namespace DevGPT.NewAPI
{
    public class Embedding
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Checksum { get; private set; }
        public EmbeddingData Embeddings { get; set; }
        public Embedding(string name, string path, string checksum, EmbeddingData embeddings)
        {
            Name = name;
            Path = path;
            Checksum = checksum;
            Embeddings = embeddings;
        }
    }
}