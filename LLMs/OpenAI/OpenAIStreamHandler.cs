using OpenAI.Chat;
using SharpToken;
using System.Text;

public class OpenAIStreamHandler
{
    public async Task<string> HandleStream(
    Action<string> onChunkReceived, IAsyncEnumerable<StreamingChatCompletionUpdate> stream)
    {
        var fullResponse = new StringBuilder();
        await foreach (var chunk in stream)
        {
            if (chunk != null)
            {
                HandleChunk(chunk, onChunkReceived, fullResponse);
                if (chunk.FinishReason != null)
                    break;
            }
        }
        return fullResponse.ToString();
    }

    protected void HandleChunk(StreamingChatCompletionUpdate chunk, Action<string> onChunkReceived, StringBuilder fullResponse)
    {
        var text = string.Join(" ", chunk.ContentUpdate.Select(u => u.Text).ToList());
        if (text != null)
        {
            fullResponse.Append(text);
            onChunkReceived?.Invoke(text);
        }
    }
}
