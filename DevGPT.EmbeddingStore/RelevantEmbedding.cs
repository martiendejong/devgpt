public class RelevantEmbedding
{
    public double Similarity { get; set; }
    public string StoreName { get; set; }
    public EmbeddingInfo Document { get; set; }
    public Func<string, Task<string>> GetText { get; set; }
}
