using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace DevGPT.LLMs;

/// <summary>
/// Handles streaming responses from Semantic Kernel and converts them to DevGPT format
/// </summary>
public class SemanticKernelStreamHandler
{
    /// <summary>
    /// Handle streaming chat completion with callback for each chunk
    /// </summary>
    public async Task<string> HandleStreamAsync(
        IAsyncEnumerable<StreamingChatMessageContent> stream,
        Action<string> onChunkReceived,
        TokenUsageInfo tokenUsage,
        CancellationToken cancellationToken = default)
    {
        var fullResponse = new StringBuilder();
        StreamingChatMessageContent? lastChunk = null;

        await foreach (var chunk in stream.WithCancellation(cancellationToken))
        {
            if (chunk != null)
            {
                lastChunk = chunk;

                // Extract text content from chunk
                var text = chunk.Content ?? string.Empty;

                if (!string.IsNullOrEmpty(text))
                {
                    fullResponse.Append(text);
                    onChunkReceived?.Invoke(text);
                }

                // Update token usage if available in chunk metadata
                chunk.UpdateTokenUsage(tokenUsage);
            }
        }

        return fullResponse.ToString();
    }

    /// <summary>
    /// Handle streaming with tool calls support (for future implementation)
    /// </summary>
    public async Task<(string Response, List<FunctionCallContent> ToolCalls)> HandleStreamWithToolsAsync(
        IAsyncEnumerable<StreamingChatMessageContent> stream,
        Action<string> onChunkReceived,
        TokenUsageInfo tokenUsage,
        CancellationToken cancellationToken = default)
    {
        var fullResponse = new StringBuilder();
        var toolCalls = new List<FunctionCallContent>();
        StreamingChatMessageContent? lastChunk = null;

        await foreach (var chunk in stream.WithCancellation(cancellationToken))
        {
            if (chunk != null)
            {
                lastChunk = chunk;

                // Extract text content
                var text = chunk.Content ?? string.Empty;
                if (!string.IsNullOrEmpty(text))
                {
                    fullResponse.Append(text);
                    onChunkReceived?.Invoke(text);
                }

                // Extract function calls if present
                var functionCalls = chunk.Items.OfType<FunctionCallContent>();
                toolCalls.AddRange(functionCalls);

                // Update token usage
                chunk.UpdateTokenUsage(tokenUsage);
            }
        }

        return (fullResponse.ToString(), toolCalls);
    }
}
