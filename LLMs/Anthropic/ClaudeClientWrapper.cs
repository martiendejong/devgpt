using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DevGPT.LLMs.Anthropic;

public class ClaudeClientWrapper : ILLMClient
{
    private readonly AnthropicConfig _config;
    private readonly HttpClient _http;

    public ClaudeClientWrapper(AnthropicConfig config)
    {
        _config = config;
        _http = new HttpClient
        {
            BaseAddress = new Uri(config.Endpoint)
        };
        _http.DefaultRequestHeaders.Add("x-api-key", _config.ApiKey);
        _http.DefaultRequestHeaders.Add("anthropic-version", _config.ApiVersion);
    }

    // Anthropic does not currently expose embeddings compatible with this interface in this codebase
    public Task<Embedding> GenerateEmbedding(string data)
        => throw new NotSupportedException("Anthropic Claude embeddings are not supported by this client.");

    // Anthropic Messages API basic request body
    private sealed record AnthropicMessageRequest(string model, int max_tokens, object[] messages, string? system = null);

    private sealed record AnthropicContentBlock(string type, string text);

    private sealed record AnthropicMessage(string role, AnthropicContentBlock[] content);

    private sealed record AnthropicMessageResponse(AnthropicContentBlock[] content);

    private static string BuildJsonFormatInstruction<ResponseType>() where ResponseType : ChatResponse<ResponseType>, new()
    {
        // Align with OpenAI wrapper formatting instruction for parity
        return $"YOUR OUTPUT WILL ALWAYS BE ONLY A JSON RESPONSE IN THIS FORMAT AND NOTHING ELSE: {ChatResponse<ResponseType>.Signature} EXAMPLE: {JsonSerializer.Serialize(ChatResponse<ResponseType>.Example)}";
    }

    private static (string? system, List<AnthropicMessage> msgs) MapMessages(List<DevGPTChatMessage> messages)
    {
        // Gather system as a single string (join if multiple)
        var systemParts = messages
            .Where(m => m.Role == DevGPTMessageRole.System || m.Role.Role == DevGPTMessageRole.System.Role)
            .Select(m => m.Text)
            .ToList();
        string? system = systemParts.Count > 0 ? string.Join("\n\n", systemParts) : null;

        var mapped = new List<AnthropicMessage>();
        foreach (var m in messages)
        {
            if (m.Role == DevGPTMessageRole.System || m.Role.Role == DevGPTMessageRole.System.Role)
                continue; // handled via system field

            var role = (m.Role == DevGPTMessageRole.Assistant || m.Role.Role == DevGPTMessageRole.Assistant.Role) ? "assistant" : "user";
            mapped.Add(new AnthropicMessage(
                role,
                [ new AnthropicContentBlock("text", m.Text) ]
            ));
        }
        return (system, mapped);
    }

    private async Task<string> CallClaude(List<DevGPTChatMessage> messages, CancellationToken cancel, IToolsContext? toolsContext = null)
    {
        var (system, mapped) = MapMessages(messages);
        var id = Guid.NewGuid().ToString();
        toolsContext?.SendMessage?.Invoke(id, "LLM Request (Claude)", string.Join("\n", messages.Select(m => $"{m.Role?.Role}: {m.Text}")));
        var req = new AnthropicMessageRequest(
            model: _config.Model,
            max_tokens: 1024,
            messages: mapped.Cast<object>().ToArray(),
            system: system
        );

        var json = JsonSerializer.Serialize(req, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync("/v1/messages", content, cancel);
        var respText = await resp.Content.ReadAsStringAsync(cancel);
        if (!resp.IsSuccessStatusCode)
        {
            throw new Exception($"Claude error: {resp.StatusCode}: {respText}");
        }
        try
        {
            var parsed = JsonSerializer.Deserialize<AnthropicMessageResponse>(respText);
            var firstText = parsed?.content?.FirstOrDefault(c => c.type == "text")?.text ?? string.Empty;
            toolsContext?.SendMessage?.Invoke(id, "LLM Response (Claude)", firstText);
            return firstText;
        }
        catch
        {
            // Fallback to raw
            toolsContext?.SendMessage?.Invoke(id, "LLM Response (Claude)", respText);
            return respText;
        }
    }

    public async Task<string> GetResponse(List<DevGPTChatMessage> messages, DevGPTChatResponseFormat responseFormat, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel)
    {
        // images/tooling ignored in this basic implementation
        return await CallClaude(messages, cancel, toolsContext);
    }

    public async Task<ResponseType?> GetResponse<ResponseType>(List<DevGPTChatMessage> messages, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel) where ResponseType : ChatResponse<ResponseType>, new()
    {
        // Inject formatting instruction as a System message before the final user prompt for better adherence
        var withFormat = new List<DevGPTChatMessage>(messages);
        var instruction = BuildJsonFormatInstruction<ResponseType>();
        // Insert near the end but before last message if possible
        var insertIndex = Math.Max(0, withFormat.Count - 1);
        withFormat.Insert(insertIndex, new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = instruction });

        var text = await CallClaude(withFormat, cancel, toolsContext);
        try
        {
            return JsonSerializer.Deserialize<ResponseType>(text);
        }
        catch
        {
            // Try to salvage by trimming to first JSON object if present
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                var json = text.Substring(start, end - start + 1);
                try { return JsonSerializer.Deserialize<ResponseType>(json); } catch { }
            }
            throw;
        }
    }

    public async Task<string> GetResponseStream(List<DevGPTChatMessage> messages, Action<string> onChunkReceived, DevGPTChatResponseFormat responseFormat, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel)
    {
        // Simple chunking as a placeholder; Anthropic SSE can be added later
        var full = await GetResponse(messages, responseFormat, toolsContext, images, cancel);
        foreach (var chunk in Chunk(full, 60))
            onChunkReceived(chunk);
        return full;
    }

    public async Task<ResponseType?> GetResponseStream<ResponseType>(List<DevGPTChatMessage> messages, Action<string> onChunkReceived, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel) where ResponseType : ChatResponse<ResponseType>, new()
    {
        var text = await GetResponseStream(messages, onChunkReceived, DevGPTChatResponseFormat.Json, toolsContext, images, cancel);
        return JsonSerializer.Deserialize<ResponseType>(text);
    }

    public Task<DevGPTGeneratedImage> GetImage(string prompt, DevGPTChatResponseFormat responseFormat, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel)
        => throw new NotSupportedException("Claude does not generate images in this client.");

    private static IEnumerable<string> Chunk(string s, int size)
    {
        for (int i = 0; i < s.Length; i += size)
            yield return s.Substring(i, Math.Min(size, s.Length - i));
    }
}
