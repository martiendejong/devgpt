using System;using System.Collections.Generic;using System.Linq;using System.Threading.Tasks;
using OpenAI_API;
using OpenAI_API.Embedding;
public class EmbeddingHandler 
{ 
    private readonly string openaiApiKey; 
    public EmbeddingHandler(string apiKey) 
    { openaiApiKey = apiKey; } 
    public async Task<Dictionary<string, List<double>>> GenerateEmbeddings(Dictionary<string, string> documentContents) { 
        var openai = new OpenAIAPI(openaiApiKey); 
        var embeddings = new Dictionary<string, List<double>>(); 
        foreach (var document in documentContents) { 
            try { 
                var embedding = await FetchEmbedding(openai, document.Key, document.Value); 
                embeddings[document.Key] = embedding; 
            } catch (Exception ex) { 
                Console.WriteLine($"Cannot embed file {document.Key}"); 
                Console.WriteLine(ex.Message); 
            } 
        } 
        return embeddings; 
    } 
    private async static Task<List<double>> FetchEmbedding(OpenAIAPI openai, string key, string text) { 
        var response = await openai.Embeddings.CreateEmbeddingAsync(new EmbeddingRequest { 
            Input = text, Model = "text-embedding-ada-002" }); 
        return new List<double>(response.Data[0].Embedding.Select(e => (double)e)); 
    } 
}