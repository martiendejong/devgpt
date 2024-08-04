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
using System.Net.Sockets;

public partial class ProjectUpdater
{
    private string openaiApiKey;
    private AppBuilderConfig config;
    private CodeUpdater codeUpdater;

    public ProjectUpdater(AppBuilderConfig config)
    {
        this.config = config;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
        openaiApiKey = configuration["OpenAI:ApiKey"];
        codeUpdater = new CodeUpdater(config);
    }

    #region answer question

    public async Task<string> AnswerQuestion()
    {
        if (config.GenerateEmbeddings)
        {
            var embeddings = new EmbeddingGenerator(openaiApiKey);
            await embeddings.GenerateAndStoreEmbeddings(config.FolderPath, config.EmbeddingsFile);
        }

        return await AnswerQuestion(config.FolderPath, config.EmbeddingsFile, config.Query, config.UseHistory ? config.HistoryFile : "");
    }

    private async Task<string> AnswerQuestion(string folderPath, string embeddingsFile, string query, string historyFile = null)
    {
        var openai = new OpenAIAPI(openaiApiKey);
        List<string> topSimilarDocumentsContent = await GetRelevantDocuments(folderPath, embeddingsFile, query, openai);

        List<ChatMessage>? history = await GetHistory(historyFile);

        var mostRelevantDocContent = string.Join("\n\n", topSimilarDocumentsContent);
        var message = await AnswerQuestionFromDocument(openai, mostRelevantDocContent, query, history.ToArray());

        return message;
    }

    private async Task<string> AnswerQuestionFromDocument(OpenAIAPI openai, string document, string query, ChatMessage[] history)
    {
        string content;
        try
        {
            var systemInstructions = config.SystemInstructions1;
            var historyStr = history.Any() ? $"\n\nAnd the conversation history:\n\n{history.Select(h => $"{h.Role}: {h.TextContent}\n")}." : "";

            var response = await openai.Chat.CreateChatCompletionAsync(new ChatRequest
            {
                Messages = new ChatMessage[] { new ChatMessage(ChatMessageRole.User, $"{systemInstructions}\nBased on the following document:\n\n{document}{historyStr}\n\nAnswer the following query:\n\n{query}") },
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

    #endregion

    #region update code

    public async Task<string> UpdateCode()
    {
        if (config.GenerateEmbeddings)
        {
            var embeddings = new EmbeddingGenerator(openaiApiKey);
            await embeddings.GenerateAndStoreEmbeddings(config.FolderPath, config.EmbeddingsFile);
        }

        var result = await GetUpdateCodeResponse(config.FolderPath, config.EmbeddingsFile, config.Query, config.UseHistory ? config.HistoryFile : "");

        return await codeUpdater.UpdateProject(result);
    }

    private async Task<Response> GetUpdateCodeResponse(string folderPath, string embeddingsFile, string query, string historyFile = null)
    {
        var openai = new OpenAIAPI(openaiApiKey);
        List<string> topSimilarDocumentsContent = await GetRelevantDocuments(folderPath, embeddingsFile, query, openai);

        List<ChatMessage>? history = await GetHistory(historyFile);

        var mostRelevantDocContent = string.Join("\n\n", topSimilarDocumentsContent);
        var queryResponse = await GetUpdateCodeResponseFromDocument(openai, mostRelevantDocContent, query, history.ToArray());

        return queryResponse;
    }
    private async Task<Response> GetUpdateCodeResponseFromDocument(OpenAIAPI openai, string document, string query, ChatMessage[] history)
    {
        string content = "";
        bool isComplete = false;
        int continuationCount = 0;
        const int maxRetries = 3;

        while (!isComplete && continuationCount < maxRetries)
        {
            try
            {
                var systemInstructions = config.SystemInstructions2;
                var files = new ProjectLoader().GetFiles(config.FolderPath);
                var formattingInstructions = $"YOUR OUTPUT WILL ALWAYS BE ONLY A JSON RESPONSE IN THIS FORMAT AND NOTHING ELSE: {{ \"message\": \"a description of what is changed\", \"changes\": [{{ \"file\": \"the path of the file that is changed\", \"content\": \"the content of the WHOLE file. ALWAYS WRITE THE WHOLE FILE.\" }}], \"deletions\": [\"file that is deleted. empty array if no deletions\"] }}";
                var historyStr = history.Any() ? $"\n\nAnd the conversation history:\n\n{string.Join('\n', history.Select(h => $"{h.Role.ToString()}: {h.TextContent}\n"))}." : "";

                var fullQuery = "";
                if (continuationCount > 0)
                {
                    fullQuery = $"Continue this message: {systemInstructions}\nBased on the following documents:\n\nFiles:{files}\n\n{document}{historyStr}\n\nAnswer the following query:\n\n{query}\n\nContinue your response:\n {content}";
                }
                else
                {
                    fullQuery = $"{systemInstructions}\n{formattingInstructions}\nBased on the following documents:\n\nFiles:{files}\n\n{document}{historyStr}\n\nAnswer the following query:\n\n{query}\n{formattingInstructions}";
                }

                var response = await openai.Chat.CreateChatCompletionAsync(new ChatRequest
                {
                    Messages = new ChatMessage[] { new ChatMessage(ChatMessageRole.User, fullQuery) },
                    Model = "gpt-4o",
                    ResponseFormat = ChatRequest.ResponseFormats.JsonObject
                });

                content += response.Choices[0].Message.TextContent.Trim();
                isComplete = response.Choices[0].FinishReason != "length";
                continuationCount++;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw new Exception("Error getting the message from OpenAI");
            }
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

    #endregion

    #region update with plan

    public async Task<string> RunWithPlan()
    {
        if (config.GenerateEmbeddings)
        {
            var embeddings = new EmbeddingGenerator(openaiApiKey);
            await embeddings.GenerateAndStoreEmbeddings(config.FolderPath, config.EmbeddingsFile);
        }

        var result = await GetRunWithPlanResponse(config.FolderPath, config.EmbeddingsFile, config.Query, config.UseHistory ? config.HistoryFile : "");

        return;
    }

    private async Task<Response> GetRunWithPlanResponse(string folderPath, string embeddingsFile, string query, string historyFile = null)
    {
        var openai = new OpenAIAPI(openaiApiKey);
        List<string> topSimilarDocumentsContent = await GetRelevantDocuments(folderPath, embeddingsFile, query, openai);

        List<ChatMessage>? history = await GetHistory(historyFile);

        var mostRelevantDocContent = string.Join("\n\n", topSimilarDocumentsContent);
        var queryResponse = await GetRunWithPlanResponseFromDocument(openai, mostRelevantDocContent, query, history.ToArray());

        return queryResponse;
    }

    private async Task<Response> GetRunWithPlanResponseFromDocument(OpenAIAPI openai, string document, string query, ChatMessage[] history)
    {
        string content = "";
        bool isComplete = false;
        int continuationCount = 0;
        const int maxRetries = 3;

        while (!isComplete && continuationCount < maxRetries)
        {
            try
            {
                var systemInstructions = config.SystemInstructions2;
                var files = GetFiles(config.FolderPath);
                var formattingInstructions = $"YOUR OUTPUT WILL ALWAYS BE ONLY A JSON RESPONSE IN THIS FORMAT AND NOTHING ELSE: {{ \"message\": \"a description of what is changed\", \"changes\": [{{ \"file\": \"the path of the file that is changed\", \"content\": \"the content of the WHOLE file. ALWAYS WRITE THE WHOLE FILE.\" }}], \"deletions\": [\"file that is deleted. empty array if no deletions\"] }}";
                var historyStr = history.Any() ? $"\n\nAnd the conversation history:\n\n{string.Join('\n', history.Select(h => $"{h.Role.ToString()}: {h.TextContent}\n"))}." : "";

                var fullQuery = "";
                if (continuationCount > 0)
                {
                    fullQuery = $"Continue this message: {systemInstructions}\nBased on the following documents:\n\nFiles:{files}\n\n{document}{historyStr}\n\nAnswer the following query:\n\n{query}\n\nContinue your response:\n {content}";
                }
                else
                {
                    fullQuery = $"{systemInstructions}\n{formattingInstructions}\nBased on the following documents:\n\nFiles:{files}\n\n{document}{historyStr}\n\nAnswer the following query:\n\n{query}\n{formattingInstructions}";
                }

                var response = await openai.Chat.CreateChatCompletionAsync(new ChatRequest
                {
                    Messages = new ChatMessage[] { new ChatMessage(ChatMessageRole.User, fullQuery) },
                    Model = "gpt-4o",
                    ResponseFormat = ChatRequest.ResponseFormats.JsonObject
                });

                content += response.Choices[0].Message.TextContent.Trim();
                isComplete = response.Choices[0].FinishReason != "length";
                continuationCount++;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw new Exception("Error getting the message from OpenAI");
            }
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
    #endregion

    #region history

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
                Console.WriteLine($"Cannot read history file {historyFile}");
                history = new List<ChatMessage>();
            }
        }

        return history;
    }

    #endregion

    #region relevancesearch

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

    #endregion
}