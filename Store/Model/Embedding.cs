namespace DevGPT.NewAPI
{
    public class EmbeddingI
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Checksum { get; private set; }
        public Embedding Embeddings { get; set; }
        public EmbeddingI(string name, string path, string checksum, Embedding embeddings)
        {
            Name = name;
            Path = path;
            Checksum = checksum;
            Embeddings = embeddings;
        }
    }
}