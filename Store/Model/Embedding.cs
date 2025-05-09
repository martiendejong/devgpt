namespace Store.Model
{
    public class EmbeddingI
    {
        public string Id { get; set; }
        public float[] Vector { get; set; }
        public string MetadataJson { get; set; }
    }
}
