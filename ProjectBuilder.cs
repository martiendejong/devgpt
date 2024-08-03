using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAI_API;
using MathNet.Numerics.LinearAlgebra;
using OpenAI_API.Embedding;
using OpenAI_API.Completions;
using OpenAI_API.Chat;
using DevGPT;
using Microsoft.Extensions.Configuration;

public class ProjectBuilder
{
    private string openaiApiKey;
    private AppBuilderConfig config;

    public ProjectBuilder(AppBuilderConfig config)
    {
        this.config = config;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
        openaiApiKey = configuration["OpenAI:ApiKey"];
    }

    public async Task<string> AnswerQuestion()
    {
        if (config.GenerateEmbeddings)
        {
            await CreateEmbeddings(config.FolderPath, config.EmbeddingsFile);
        }

        return await AnswerQuestion(config.FolderPath, config.EmbeddingsFile, config.Query, config.UseHistory? config.HistoryFile : "");
    }

    public async Task<string> UpdateCode()
    {
        if (config.GenerateEmbeddings)
        {
            await CreateEmbeddings(config.FolderPath, config.EmbeddingsFile);
        }

        // Step 2: Answer User Query
        var result = await GetUpdateCodeResponse(config.FolderPath, config.EmbeddingsFile, config.Query, config.UseHistory ? config.HistoryFile : "");
        Console.WriteLine(result.Message);
        result.Changes.ForEach(item =>
        {
            var filePath = $@"{config.FolderPath}\{item.File}";
            var file = new FileInfo(filePath);
            if (!file.Directory.Exists)
            {
                Directory.CreateDirectory(file.Directory.FullName);
            }
            File.WriteAllText($@"{config.FolderPath}\{item.File}", item.Content);
        });
        result.Deletions.ForEach(item =>
        {
            File.Delete($@"{config.FolderPath}\{item}");
        });
        return result.Message;
    }

    private async Task CreateEmbeddings(string folderPath, string embeddingsFile)
    {
        var openai = new OpenAIAPI(openaiApiKey);
        var documents = new Dictionary<string, string>();

        ProjectLoader loader = new ProjectLoader();
        var allFiles = loader.GetFiles(folderPath);
        foreach (var file in allFiles)
        {
            var relativePath = Path.GetRelativePath(folderPath, file);

            if (File.Exists(file))
            {
                documents[relativePath] = await File.ReadAllTextAsync(file);
            }
        }

        var embeddings = new Dictionary<string, List<double>>();

        foreach (var doc in documents)
        {
            try
            {
                var embedding = await GetEmbedding(openai, $@"{doc.Key}: {doc.Value}");
                embeddings[doc.Key] = embedding;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Cannot embed file {doc.Key}");
                Console.WriteLine(ex.Message);
            }
        }

        string json = JsonConvert.SerializeObject(embeddings, Formatting.Indented);
        await File.WriteAllTextAsync(embeddingsFile, json);

        Console.WriteLine($@"Embeddings saved to {embeddingsFile}");
    }

    private async Task<List<double>> GetEmbedding(OpenAIAPI openai, string text)
    {
        var response = await openai.Embeddings.CreateEmbeddingAsync(new EmbeddingRequest { Input = text, Model = "text-embedding-ada-002" });
        return new List<double>(response.Data[0].Embedding.Select(e => (double)e));
    }


    private async Task<string> AnswerQuestion(string folderPath, string embeddingsFile, string query, string historyFile = null)
    {
        var openai = new OpenAIAPI(openaiApiKey);
        List<string> topSimilarDocumentsContent = await GetRelevantDocuments(folderPath, embeddingsFile, query, openai);

        List<ChatMessage>? history = await GetHistory(historyFile);

        var mostRelevantDocContent = string.Join("\n\n", topSimilarDocumentsContent);
        var message = await AnswerQuestionFromDocument(openai, mostRelevantDocContent, query, history.ToArray());

        //await UpdateHistory(historyFile, history, message);

        return message;
    }

    private async Task<Response> GetUpdateCodeResponse(string folderPath, string embeddingsFile, string query, string historyFile = null)
    {
        var openai = new OpenAIAPI(openaiApiKey);
        List<string> topSimilarDocumentsContent = await GetRelevantDocuments(folderPath, embeddingsFile, query, openai);

        List<ChatMessage>? history = await GetHistory(historyFile);

        var mostRelevantDocContent = string.Join("\n\n", topSimilarDocumentsContent);
        var queryResponse = await GetUpdateCodeResponseFromDocument(openai, mostRelevantDocContent, query, history.ToArray());

        //await UpdateHistory(historyFile, history, queryResponse);

        return queryResponse;
    }

    public class HistoryEntry
    {
        public string Role {  get; set; }
        public string Content {  get; set; }
    }

    private static async Task UpdateHistory(string historyFile, List<ChatMessage> history, Response queryResponse)
    {
        if (historyFile != null)
        {
            history.Add(new ChatMessage(ChatMessageRole.User, queryResponse.Message));
            var entries = history.Select(h => new HistoryEntry { Role = h.Role.ToString(), Content = h.TextContent });
            await File.WriteAllTextAsync(historyFile, JsonConvert.SerializeObject(entries));
        }
    }

    private static async Task<List<ChatMessage>> GetHistory(string historyFile)
    {
        var history = new List<ChatMessage>();
        if (historyFile != null && File.Exists(historyFile))
        {
            try
            {
                var entries = JsonConvert.DeserializeObject<List<HistoryEntry>>(await File.ReadAllTextAsync(historyFile));
                history = entries.Select(e => new ChatMessage(ChatMessageRole.FromString(e.Role), e.Content)).ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine($@"Cannot read history file {historyFile}");
                history = new List<ChatMessage>();
            }
        }

        return history;
    }

    private async Task<List<string>> GetRelevantDocuments(string folderPath, string embeddingsFile, string query, OpenAIAPI openai)
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
            topSimilarDocumentsContent.Add($@"{docName}:\n\n{docContent}");
        }

        return topSimilarDocumentsContent;
    }
        
    private async Task<string> AnswerQuestionFromDocument(OpenAIAPI openai, string document, string query, ChatMessage[] history)
    {
        string content;
        try
        {
            var systemInstructions = config.SystemInstructions1;
            var historyStr = history.Any() ? $@"\n\nAnd the conversation history:\n\n{history.Select(h => $"{h.Role}: {h.TextContent}\n")}." : "";

            var response = await openai.Chat.CreateChatCompletionAsync(new ChatRequest
            {
                Messages = new ChatMessage[] { new ChatMessage(ChatMessageRole.User, $@"{systemInstructions}\nBased on the following document:\n\n{document}{historyStr}\n\nAnswer the following query:\n\n{query}") },
                Model = "gpt-4o",
                ResponseFormat = ChatRequest.ResponseFormats.Text
            });

            content = response.Choices[0].Message.TextContent.Trim();
            return content;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw new Exception("Error getting the message from OpenAI");
        }
    }

    private async Task<Response> GetUpdateCodeResponseFromDocument(OpenAIAPI openai, string document, string query, ChatMessage[] history)
    {
        string content;
        try
        {
            var systemInstructions = config.SystemInstructions2;
            var formattingInstructions = $@"YOUR OUTPUT WILL ALWAYS BE ONLY A JSON RESPONSE IN THIS FORMAT AND NOTHING ELSE: {{ ""message"": ""a description of what is changed"", ""changes"": [{{ ""file"": ""the path of the file that is changed"", ""content"": ""the content of the WHOLE file. ALWAYS WRITE THE WHOLE FILE."" }}], ""deletions"": [""file that is deleted. empty array if no deletions""] }}";
            var historyStr = history.Any() ? $@"\n\nAnd the conversation history:\n\n{string.Join('\n', history.Select(h => $"{h.Role.ToString()}: {h.TextContent}\n"))}." : "";
            var fullQuery = $@"{systemInstructions}\n{formattingInstructions}\nBased on the following document:\n\n{document}{historyStr}\n\nAnswer the following query:\n\n{query}\n{formattingInstructions}";

            var response = await openai.Chat.CreateChatCompletionAsync(new ChatRequest
            {
                Messages = new ChatMessage[] { new ChatMessage(ChatMessageRole.User, fullQuery) },
                Model = "gpt-4o",
                ResponseFormat = ChatRequest.ResponseFormats.JsonObject
            });

            content = response.Choices[0].Message.TextContent.Trim();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw new Exception("Error getting the message from OpenAI");
        }

        try 
        {
            var start = content.IndexOf('{');
            var end = content.LastIndexOf('}');

            var jsonPart = content.Substring(start, end - start + 1);
            var json = JsonConvert.DeserializeObject<Response>(jsonPart);

            return json;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(content);
            throw new Exception("Error parsing the message from OpenAI");
        }
    }

    private double CosineSimilarity(Vector<double> v1, Vector<double> v2)
    {
        return v1.DotProduct(v2) / (v1.L2Norm() * v2.L2Norm());
    }
}
