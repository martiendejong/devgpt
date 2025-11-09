using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DevGPT.LLMs.Gemini;

public class GeminiClientWrapper : ILLMClient
{
    private readonly GeminiConfig _config;
    private readonly HttpClient _http;

    public GeminiClientWrapper(GeminiConfig config)
    {
        _config = config;
        _http = new HttpClient
        {
            BaseAddress = new Uri(config.Endpoint)
        };
    }

    // Embeddings not implemented for Gemini in this wrapper
    public Task<Embedding> GenerateEmbedding(string data)
        => throw new NotSupportedException("Gemini embeddings are not supported by this client.");

    private sealed record GeminiPart(string text);
    private sealed record GeminiContent(string role, GeminiPart[] parts);
    private sealed record GeminiRequest(GeminiContent[] contents, object? system_instruction = null);

    private sealed record GeminiUsage(int promptTokenCount, int candidatesTokenCount, int totalTokenCount);

    private sealed record GeminiCandidateContentPart(string? text);
    private sealed record GeminiCandidateContent(GeminiCandidateContentPart[] parts);
    private sealed record GeminiCandidate(GeminiCandidateContent content);
    private sealed record GeminiResponse(GeminiCandidate[] candidates, GeminiUsage? usageMetadata);

    private static (object? system, List<GeminiContent> msgs) MapMessages(List<DevGPTChatMessage> messages)
    {
        var systemParts = messages
            .Where(m => m.Role == DevGPTMessageRole.System || m.Role.Role == DevGPTMessageRole.System.Role)
            .Select(m => m.Text)
            .ToList();
        object? system = null;
        if (systemParts.Count > 0)
        {
            system = new { parts = systemParts.Select(t => new { text = t }).ToArray() };
        }

        var mapped = new List<GeminiContent>();
        foreach (var m in messages)
        {
            if (m.Role == DevGPTMessageRole.System || m.Role.Role == DevGPTMessageRole.System.Role)
                continue;
            var role = (m.Role == DevGPTMessageRole.Assistant || m.Role.Role == DevGPTMessageRole.Assistant.Role) ? "model" : "user";
            mapped.Add(new GeminiContent(role, [ new GeminiPart(m.Text) ]));
        }
        return (system, mapped);
    }

    private async Task<(string text, TokenUsageInfo usage)> CallGemini(List<DevGPTChatMessage> messages, CancellationToken cancel, IToolsContext? tools = null)
    {
        var (system, mapped) = MapMessages(messages);
        var id = Guid.NewGuid().ToString();
        tools?.SendMessage?.Invoke(id, "LLM Request (Gemini)", string.Join("\n", messages.Select(m => $"{m.Role?.Role}: {m.Text}")));

        var req = new GeminiRequest(mapped.ToArray(), system);
        var json = JsonSerializer.Serialize(req);
        var uri = $"/models/{_config.Model}:generateContent?key={_config.ApiKey}";
        using var resp = await _http.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"), cancel);
        var respText = await resp.Content.ReadAsStringAsync(cancel);
        if (!resp.IsSuccessStatusCode)
        {
            throw new Exception($"Gemini error: {resp.StatusCode}: {respText}");
        }

        GeminiResponse? parsed = null;
        try { parsed = JsonSerializer.Deserialize<GeminiResponse>(respText); } catch { }

        var text = parsed?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text ?? respText;
        var usage = ExtractTokenUsage(parsed);
        tools?.SendMessage?.Invoke(id, "LLM Response (Gemini)", text);
        return (text, usage);
    }

    private TokenUsageInfo ExtractTokenUsage(GeminiResponse? response)
    {
        if (response?.usageMetadata == null)
            return new TokenUsageInfo(0, 0, 0, 0, _config.Model);

        var input = response.usageMetadata.promptTokenCount;
        var output = response.usageMetadata.candidatesTokenCount;

        decimal inputCost = CalculateCost(_config.Model, input, true);
        decimal outputCost = CalculateCost(_config.Model, output, false);
        return new TokenUsageInfo(input, output, inputCost, outputCost, _config.Model);
    }

    private decimal CalculateCost(string model, int tokens, bool isInput)
    {
        var m = model.ToLower();
        decimal pricePerMillion = m.Contains("1.5-pro") ? (isInput ? 3m : 15m)
                                 : m.Contains("1.5-flash") ? (isInput ? 0.35m : 0.53m)
                                 : (isInput ? 1m : 2m);
        return (tokens / 1_000_000m) * pricePerMillion;
    }

    public async Task<LLMResponse<string>> GetResponse(List<DevGPTChatMessage> messages, DevGPTChatResponseFormat responseFormat, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel)
    {
        var (text, usage) = await CallGemini(messages, cancel, toolsContext);
        return new LLMResponse<string>(text, usage);
    }

    public async Task<LLMResponse<ResponseType?>> GetResponse<ResponseType>(List<DevGPTChatMessage> messages, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel) where ResponseType : ChatResponse<ResponseType>, new()
    {
        // Add JSON format instruction similar to other clients
        var withFormat = new List<DevGPTChatMessage>(messages);
        var instruction = $"YOUR OUTPUT WILL ALWAYS BE ONLY A JSON RESPONSE IN THIS FORMAT AND NOTHING ELSE: {ChatResponse<ResponseType>.Signature} EXAMPLE: {JsonSerializer.Serialize(ChatResponse<ResponseType>.Example)}";
        var insert = Math.Max(0, withFormat.Count - 1);
        withFormat.Insert(insert, new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = instruction });

        var (text, usage) = await CallGemini(withFormat, cancel, toolsContext);
        try
        {
            var result = JsonSerializer.Deserialize<ResponseType>(text);
            return new LLMResponse<ResponseType?>(result, usage);
        }
        catch
        {
            // Try to trim to first JSON
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                var json = text.Substring(start, end - start + 1);
                try
                {
                    var result = JsonSerializer.Deserialize<ResponseType>(json);
                    return new LLMResponse<ResponseType?>(result, usage);
                }
                catch { }
            }
            throw;
        }
    }

    public async Task<LLMResponse<string>> GetResponseStream(List<DevGPTChatMessage> messages, Action<string> onChunkReceived, DevGPTChatResponseFormat responseFormat, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel)
    {
        var response = await GetResponse(messages, responseFormat, toolsContext, images, cancel);
        foreach (var chunk in Chunk(response.Result, 60)) onChunkReceived(chunk);
        return response;
    }

    public async Task<LLMResponse<ResponseType?>> GetResponseStream<ResponseType>(List<DevGPTChatMessage> messages, Action<string> onChunkReceived, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel) where ResponseType : ChatResponse<ResponseType>, new()
    {
        var response = await GetResponseStream(messages, onChunkReceived, DevGPTChatResponseFormat.Json, toolsContext, images, cancel);
        var result = JsonSerializer.Deserialize<ResponseType>(response.Result);
        return new LLMResponse<ResponseType?>(result, response.TokenUsage);
    }

    public Task<LLMResponse<DevGPTGeneratedImage>> GetImage(string prompt, DevGPTChatResponseFormat responseFormat, IToolsContext? toolsContext, List<ImageData>? images, CancellationToken cancel)
        => throw new NotSupportedException("Gemini image generation is not supported by this client.");

    private static IEnumerable<string> Chunk(string s, int size)
    {
        for (int i = 0; i < s.Length; i += size)
            yield return s.Substring(i, Math.Min(size, s.Length - i));
    }
}

