using System.IO;
using Newtonsoft.Json;
using OpenAI_API;
using MathNet.Numerics.LinearAlgebra;
using OpenAI_API.Embedding;

public class RelevanceService
{
    private OpenAIAPI openai;
    public RelevanceService(OpenAIAPI _openai)
    {
        openai = _openai;
    }

    public async Task<List<string>> GetRelevantDocuments(string folderPath, string embeddingsFile, string query)
    {
        var embeddings = JsonConvert.DeserializeObject<Dictionary<string, List<double>>>(await File.ReadAllTextAsync(embeddingsFile));

        var queryEmbedding = await GetEmbedding(openai, query);
        var queryVector = Vector<double>.Build.DenseOfArray(queryEmbedding.ToArray());

        var similarities = new List<(double similarity, string documentName)>();

        foreach (var kvp in embeddings)
        {
            var docVector = Vector<double>.Build.DenseOfArray(kvp.Value.ToArray());
            var similarity = CosineSimilarity(queryVector, docVector);

            similarities.Add((similarity, kvp.Key));
        }

        similarities.Sort((x, y) => y.similarity.CompareTo(x.similarity));

        const int numTopDocuments = 8;
        var topSimilarDocumentsContent = new List<string>();

        for (int i = 0; i < Math.Min(numTopDocuments, similarities.Count); i++)
        {
            string docName = similarities[i].documentName;
            string docContent = await File.ReadAllTextAsync(Path.Combine(folderPath, docName));
            topSimilarDocumentsContent.Add($"{docName}:\n\n{docContent}");
        }

        return topSimilarDocumentsContent;
    }

    private double CosineSimilarity(Vector<double> v1, Vector<double> v2)
    {
        return v1.DotProduct(v2) / (v1.L2Norm() * v2.L2Norm());
    }

    private async Task<List<double>> GetEmbedding(OpenAIAPI openai, string text)
    {
        var response = await openai.Embeddings.CreateEmbeddingAsync(new EmbeddingRequest { Input = text, Model = "text-embedding-ada-002" });
        return new List<double>(response.Data[0].Embedding.Select(e => (double)e));
    }
}