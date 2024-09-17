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
        const int numTopDocuments = 8;

        List<string> docNames = await GetRelevantDocNames(embeddingsFile, query, numTopDocuments);

        List<string> topSimilarDocumentsContent = await GetDocuments(folderPath, docNames);

        return topSimilarDocumentsContent;
    }

    public async Task<List<string>> GetRelevantDocNames(string embeddingsFile, string query, int numTopDocuments)
    {
        List<(double similarity, string documentName)> similarities = await GetRelevantDocumentNamesBySimilarity(embeddingsFile, query);
        var docNames = similarities.Select(sim => sim.documentName).Take(numTopDocuments).ToList();
        return docNames;
    }

    public async Task<List<(double similarity, string documentName)>> GetRelevantDocumentNamesBySimilarity(string embeddingsFile, string query)
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
        return similarities;
    }

    public async Task<List<string>> GetDocuments(string folderPath, List<string> docNames)
    {
        var topSimilarDocumentsContent = docNames.Select(docName =>
        {
            string docContent = File.ReadAllText(Path.Combine(folderPath, docName));
            return $"{docName}:\n\n{docContent}";
        });
        return topSimilarDocumentsContent.ToList();
    }

    public async Task<List<string>> GetDocumentsRelative(string folderPath, List<string> docNames)
    {
        var topSimilarDocumentsContent = docNames.Select(docName =>
        {
            string docContent = File.ReadAllText(Path.Combine(docName));
            var relativeDocName = docName.Substring(folderPath.Length + 1);
            return $"{relativeDocName}:\n\n{docContent}";
        });
        return topSimilarDocumentsContent.ToList();
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