public class RelevantEmbedding
{
    public double Similarity { get; set; }
    public string StoreName { get; set; } = "";
    public EmbeddingInfo? Document { get; set; } = null;
<<<<<<<< HEAD:Store/EmbeddingStore/Models/RelevantEmbedding.cs
    public string? ParentDocumentKey { get; set; } = null;
========
>>>>>>>> d917293a6c55216684ce8c170f8813dd604f3c15:Store/EmbeddingStore/RelevantEmbedding.cs
    public Func<string, Task<string>>? GetText { get; set; } = null;
}
