using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using backend.Controllers;
using DevGPT.NewAPI;
using OpenAI;
using OpenAI.Chat;

public class StoreToolsContext : IToolsContext
{
    public string Model { get; set; }
    public string ApiKey { get; set; }
    public DocumentStore Store { get; set; }
    public List<Tool> Tools { get; set; }

    public StoreToolsContext(string model, string apiKey, DocumentStore store)
    {
        Model = model;
        ApiKey = apiKey;
        Store = store;
        Tools = new List<Tool>()
        {
            new Tool() 
            { 
                FunctionName = nameof(PerformWebSearch), 
                Definition = getWebSearchTool, 
                Execute = async (List<ChatMessage> messages, ChatToolCall toolCall) =>
                {
                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                    bool hasQuery = argumentsJson.RootElement.TryGetProperty("query", out JsonElement query);
                    if (hasQuery)
                    {
                        return await PerformWebSearch(query.GetString());
                    }
                    return "Invalid call, parameter query was not provided.";
                }
            },
            new Tool()
            {
                FunctionName = nameof(PerformReasoning),
                Definition = getReasoningTool,
                Execute = async (List<ChatMessage> messages, ChatToolCall toolCall) =>
                {
                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                    bool hasQuery = argumentsJson.RootElement.TryGetProperty("problem_statement", out JsonElement query);
                    if (hasQuery)
                    {
                        return await PerformReasoning(query.GetString(), messages);
                    }
                    return "Invalid call, parameter problem_statement was not provided.";
                }
            },
            new Tool()
            {
                FunctionName = nameof(PerformReadWebPage),
                Definition = getReadWebPageTool,
                Execute = async (List<ChatMessage> messages, ChatToolCall toolCall) =>
                {
                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                    bool hasQuery = argumentsJson.RootElement.TryGetProperty("url", out JsonElement query);
                    bool hasRaw = argumentsJson.RootElement.TryGetProperty("raw", out JsonElement queryRaw);
                    if (hasQuery)
                    {
                        var raw = hasRaw ? queryRaw.GetBoolean() : false;
                        return await PerformReadWebPage(query.GetString(), raw);
                    }
                    return "Invalid call, parameter url was not provided.";
                }
            },
            new Tool()
            {
                FunctionName = nameof(PerformReadStoreFile),
                Definition = getReadStoreFileTool,
                Execute = async (List<ChatMessage> messages, ChatToolCall toolCall) =>
                {
                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                    bool hasQuery = argumentsJson.RootElement.TryGetProperty("file", out JsonElement query);
                    if (hasQuery)
                    {
                        return await PerformReadStoreFile(query.GetString());
                    }
                    return "Invalid call, parameter file was not provided.";
                }
            },
            new Tool()
            {
                FunctionName = nameof(PerformGetStoreFilesList),
                Definition = getGetStoreFilesListTool,
                Execute = async (List<ChatMessage> messages, ChatToolCall toolCall) =>
                {
                    return await PerformGetStoreFilesList();
                }
            },
        };
    }




    private static readonly ChatTool getWebSearchTool = ChatTool.CreateFunctionTool(
        functionName: nameof(PerformWebSearch),
        functionDescription: "Retrieve a file",
        functionParameters: BinaryData.FromBytes("""
            {
                "type": "object",
                "properties": {
                    "file": {
                        "type": "string",
                        "description": "The relative path of the file to retrieve."
                    }
                },
                "required": [ "file" ]
            }
            """u8.ToArray())
    );

    private static readonly ChatTool getReadWebPageTool = ChatTool.CreateFunctionTool(
        functionName: nameof(PerformReadWebPage),
        functionDescription: "Read a webpage",
        functionParameters: BinaryData.FromBytes("""
            {
                "type": "object",
                "properties": {
                    "url": {
                        "type": "string",
                        "description": "The url to read"
                    },
                    "raw": {
                        "type": "boolean",
                        "description": "If the raw html needs to be returned or just the textual content. Set this to true for interpreting xml or api calls, false for when reading webpages."
                    }
                },
                "required": [ "url" ]
            }
            """u8.ToArray())
    );

    private static readonly ChatTool getReasoningTool = ChatTool.CreateFunctionTool(
        functionName: nameof(PerformReasoning),
        functionDescription: "Perform logical reasoning on the provided problem.",
        functionParameters: BinaryData.FromBytes("""
        {
            "type": "object",
            "properties": {
                "problem_statement": {
                    "type": "string",
                    "description": "The statement or problem that needs reasoning."
                }
            },
            "required": ["problem_statement"]
        }
        """u8.ToArray())
    );

    private static readonly ChatTool getGetStoreFilesListTool = ChatTool.CreateFunctionTool(
        functionName: nameof(PerformGetStoreFilesList),
        functionDescription: "Get the list of files for the current project.",
        functionParameters: BinaryData.FromBytes("""
        {
            "type": "object",
            "properties": {
            },
            "required": []
        }
        """u8.ToArray())
    );

    private static readonly ChatTool getReadStoreFileTool = ChatTool.CreateFunctionTool(
        functionName: nameof(PerformReadStoreFile),
        functionDescription: "Read a file from the current project.",
        functionParameters: BinaryData.FromBytes("""
        {
            "type": "object",
            "properties": {
                "file": {
                    "type": "string",
                    "description": "The path of the file to read."
                }
            },
            "required": ["file"]
        }
        """u8.ToArray())
    );


    

    private async Task<string> PerformReadWebPage(string url, bool raw)
    {
        return await WebPageScraper.ScrapeWebPage(url, raw);
    }

    private async Task<string> PerformGetStoreFilesList()
    {
        return Store.GetFilesList();
    }

    private async Task<string> PerformReadStoreFile(string file)
    {
        return File.ReadAllText(Store.GetFilePath(file));
    }

    private async Task<string> PerformReasoning(string problemStatement, List<ChatMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(problemStatement))
            return "No problem statement provided for reasoning.";

        try
        {
            messages.Add(new SystemChatMessage("You are a highly capable AI assistant with excellent logical reasoning abilities. Reason about the following problem statement and show your train of thought."));
            messages.Add(new AssistantChatMessage(problemStatement));

            // todo use self recursively
            // Send the reasoning task to OpenAI
            var api = new OpenAIClient(ApiKey);
            var response = await api.GetChatClient(Model).CompleteChatAsync(messages);

            // Extract and return the reasoning result
            return response.Value.Content.ToList().First().Text;
        }
        catch (Exception ex)
        {
            // Handle errors (e.g., network issues or invalid API response)
            return $"Reasoning failed: {ex.Message}";
        }
    }

    string rapidApiKey = "2373dde5d3mshbee7c5892bda826p1042c3jsn7ed3972d4df3";

    private async Task<string> PerformWebSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "No search query provided.";

        try
        {
            // Construct the URL for Contextual Web Search API (free tier via RapidAPI)
            string url = $"https://google-web-search1.p.rapidapi.com" +
                         $"?search={Uri.EscapeDataString(query)}&limit=10&related_keywords=true";

            using HttpClient httpClient = new HttpClient();

            // Add required RapidAPI headers
            httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", "google-web-search1.p.rapidapi.com");
            httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", rapidApiKey); // Replace with your RapidAPI key

            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string jsonResponse = await response.Content.ReadAsStringAsync();

            return jsonResponse;
            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            // The API returns a JSON object with a "value" property that is an array of search results.
            if (doc.RootElement.TryGetProperty("results", out JsonElement results) &&
                results.ValueKind == JsonValueKind.Array &&
                results.GetArrayLength() > 0)
            {
                return jsonResponse;
                JsonElement firstResult = results[0];
                // Optionally, you can try to extract "title" and "snippet" properties.
                if (firstResult.TryGetProperty("url", out JsonElement urlElement) &&
                    firstResult.TryGetProperty("title", out JsonElement titleElement) &&
                    firstResult.TryGetProperty("description", out JsonElement descriptionElement))
                {
                    string title = titleElement.GetString();
                    string snippet = urlElement.GetString();
                    return $"{title}: {snippet}";
                }
            }
            return "No results found.";
        }
        catch (Exception ex)
        {
            return $"Web search failed: {ex.Message}";
        }
    }
}