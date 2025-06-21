public interface IDocumentGenerator
{
    Task<string> GetResponse(string query, IEnumerable<DevGPTChatMessage>? messages, bool a, bool b, IToolsContext toolsContext, List<ImageData> images, CancellationToken cancel);
    Task<string> StreamResponse(string query, Action<string> onChunkReceived, IEnumerable<DevGPTChatMessage>? messages, bool a, bool b, IToolsContext toolsContext, List<ImageData> images, CancellationToken cancel);
    Task<ResponseType> GetResponse<ResponseType>(string query, IEnumerable<DevGPTChatMessage>? messages, bool a, bool b, IToolsContext toolsContext, List<ImageData> images, CancellationToken cancel) where ResponseType : ChatResponse<ResponseType>, new();
    Task<ResponseType> StreamResponse<ResponseType>(string query, Action<string> onChunkReceived, IEnumerable<DevGPTChatMessage>? messages, bool a, bool b, IToolsContext toolsContext, List<ImageData> images, CancellationToken cancel) where ResponseType : ChatResponse<ResponseType>, new();
    Task<string> UpdateStore(string query, IEnumerable<DevGPTChatMessage>? messages, bool a, bool b, IToolsContext toolsContext, List<ImageData> images, CancellationToken cancel);
    Task<string> StreamUpdateStore(string query, Action<string> onChunkReceived, IEnumerable<DevGPTChatMessage>? messages, bool a, bool b, IToolsContext toolsContext, List<ImageData> images, CancellationToken cancel);
}
