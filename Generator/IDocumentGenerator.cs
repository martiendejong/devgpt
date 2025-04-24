using OpenAI.Chat;

namespace DevGPT.NewAPI
{
    public interface IDocumentGenerator
    {
        Task<string> GetResponse(string query, IEnumerable<ChatMessage>? messages, bool a, bool b, IToolsContext toolsContext, List<ImageData> images);
        Task<string> StreamResponse(string query, Action<string> onChunkReceived, IEnumerable<ChatMessage>? messages, bool a, bool b, IToolsContext toolsContext, List<ImageData> images);
        Task<ResponseType> GetResponse<ResponseType>(string query, IEnumerable<ChatMessage>? messages, bool a, bool b, IToolsContext toolsContext, List<ImageData> images) where ResponseType : ChatResponse<ResponseType>, new();
        Task<ResponseType> StreamResponse<ResponseType>(string query, Action<string> onChunkReceived, IEnumerable<ChatMessage>? messages, bool a, bool b, IToolsContext toolsContext, List<ImageData> images) where ResponseType : ChatResponse<ResponseType>, new();
        Task<string> UpdateStore(string query, IEnumerable<ChatMessage>? messages, bool a, bool b, IToolsContext toolsContext, List<ImageData> images);
        Task<string> StreamUpdateStore(string query, Action<string> onChunkReceived, IEnumerable<ChatMessage>? messages, bool a, bool b, IToolsContext toolsContext, List<ImageData> images);
    }
}