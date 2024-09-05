using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Completions;
using OpenAI_API.Chat;
using DevGPT;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Net.Http;
using System.Collections;
using System.Reflection.Metadata;

public partial class ProjectUpdater
{
    private string openaiApiKey;
    private AppBuilderConfig config;
    private CodeUpdater codeUpdater;
    private RelevanceService RelevanceService;
    private OpenAIAPI OpenAIAPI => new OpenAIAPI(openaiApiKey);
    public ProjectUpdater(AppBuilderConfig config)
    {
        this.config = config;
        openaiApiKey = Settings.OpenAIApiKey;
        RelevanceService = new RelevanceService(OpenAIAPI);
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

        return await AnswerQuestion(config.Query);
    }

    private async Task<string> AnswerQuestion(string query)
    {
        List<string> topSimilarDocumentsContent = await RelevanceService.GetRelevantDocuments(config.FolderPath, config.EmbeddingsFile, query);

        List<ChatMessage>? history = await HistoryManager.GetHistory(config.UseHistory ? config.HistoryFile : null);

        var mostRelevantDocContent = string.Join("\n\n", topSimilarDocumentsContent);
        var message = await AnswerQuestionFromDocument(mostRelevantDocContent, query, history.ToArray());

        return message;
    }

    private async Task<string> AnswerQuestionFromDocument(string document, string query, ChatMessage[] history)
    {
        string content;
        try
        {
            var systemInstructions = config.SystemInstructions1;
            var historyStr = history.Any() ? $"\n\nAnd the conversation history:\n\n{history.Select(h => $"{h.Role}: {h.TextContent}\n")}." : "";
            var fullQuery = $"{systemInstructions}\nAnswer the following query:\n\n{query}\n\nBased on the following documents:\n\n{document}{historyStr}\n\n";

            var contentResponse = await GetResponseContent(OpenAIAPI, fullQuery);

            return contentResponse.Content;
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

        var result = await GetUpdateCodeResponse(config.Query);

        return await codeUpdater.UpdateProject(result);
    }

    private async Task<Response> GetUpdateCodeResponse(string query)
    {
        List<string> topSimilarDocumentsContent = await RelevanceService.GetRelevantDocuments(config.FolderPath, config.EmbeddingsFile, query);

        List<ChatMessage>? history = await HistoryManager.GetHistory(config.UseHistory ? config.HistoryFile : null);

        var mostRelevantDocContent = string.Join("\n\n", topSimilarDocumentsContent);
        var queryResponse = await GetUpdateCodeResponseFromDocument(mostRelevantDocContent, query, history.ToArray());

        return queryResponse;
    }
    private async Task<Response> GetUpdateCodeResponseFromDocument(string document, string query, ChatMessage[] history)
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
                var files = string.Join("\n", new ProjectLoader().GetFilesRelative(config.FolderPath));
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

                ResponseContent reponseContent = await GetResponseContent(OpenAIAPI, fullQuery);

                content += reponseContent.Content;
                isComplete = reponseContent.IsComplete;
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

    private static async Task<ResponseContent> GetResponseContent(OpenAIAPI openai, string fullQuery)
    {
        var response = await openai.Chat.CreateChatCompletionAsync(new ChatRequest
        {
            Messages = new ChatMessage[] { new ChatMessage(ChatMessageRole.User, fullQuery) },
            Model = "gpt-4o",
            ResponseFormat = ChatRequest.ResponseFormats.Text
        });

        var reponseContent = new ResponseContent
        {
            Content = response.Choices[0].Message.TextContent.Trim(),
            IsComplete = response.Choices[0].FinishReason != "length"
        };
        return reponseContent;
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

        var plan = await GetRunWithPlanResponse(config.Query);
        foreach(var task in plan.Tasks)
        {
            var docs = await RelevanceService.GetDocuments(config.FolderPath, task.Files);
            var mostRelevantDocContent = string.Join("\n\n", docs);

            var response = await GetUpdateCodeResponseFromDocument(mostRelevantDocContent, task.Query, new ChatMessage[] { });
            await codeUpdater.UpdateProject(response);
        }

        return string.Join("\n", plan.Tasks.Select(t => t.Title));
    }

    private async Task<Plan> GetRunWithPlanResponse(string query)
    {
        List<string> topSimilarDocumentsContent = await RelevanceService.GetRelevantDocuments(config.FolderPath, config.EmbeddingsFile, query);

        List<ChatMessage>? history = await HistoryManager.GetHistory(config.UseHistory ? config.HistoryFile : null);

        var mostRelevantDocContent = string.Join("\n\n", topSimilarDocumentsContent);
        var plan = await GetRunWithPlanResponseFromDocument(mostRelevantDocContent, query, history.ToArray());

        return plan;
    }

    private async Task<Plan> GetRunWithPlanResponseFromDocument(string document, string query, ChatMessage[] history)
    {
        string content = "";

        try
        {
            var systemInstructions = config.SystemInstructions3;
            var files = string.Join("\n", new ProjectLoader().GetFilesRelative(config.FolderPath));
            var formattingInstructions = $"YOUR OUTPUT WILL ALWAYS BE ONLY A JSON RESPONSE IN THIS FORMAT AND NOTHING ELSE: {{ \"tasks\": [{{ \"title\": \"a description of the task\", \"query\": \"the prompt for completing the task\", \"files\": [{{ \"file 1\", \"file 2\" }}] }}] }}";
            var historyStr = history.Any() ? $"\n\nAnd the conversation history:\n\n{string.Join('\n', history.Select(h => $"{h.Role.ToString()}: {h.TextContent}\n"))}." : "";

            var fullQuery = "";
            fullQuery = $"{systemInstructions}\n{formattingInstructions}\nBased on the following documents:\n\nFiles:{files}\n\n{document}{historyStr}\n\nAnswer the following query:\n\n{query}\n{formattingInstructions}";

            var contentResponse = await GetResponseContent(OpenAIAPI, fullQuery);

            content = contentResponse.Content;
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
            var json = JsonConvert.DeserializeObject<Plan>(jsonPart);

            return json;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(content);
            throw new Exception("Error parsing the message from OpenAI");
        }
    }

    private async Task<Response> GetUpdateCodeFromPlanResponse(List<string> files, string query, string historyFile = null)
    {
        List<string> topSimilarDocumentsContent = await RelevanceService.GetDocumentsRelative(config.FolderPath, files);

        List<ChatMessage>? history = await HistoryManager.GetHistory(historyFile);

        var mostRelevantDocContent = string.Join("\n\n", topSimilarDocumentsContent);
        var queryResponse = await GetUpdateCodeResponseFromDocument(mostRelevantDocContent, query, history.ToArray());

        return queryResponse;
    }

    #endregion
}