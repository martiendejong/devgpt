public interface IDocumentGenerator
{
    Task<string> GetResponse(string query, CancellationToken cancel, IEnumerable<DevGPTChatMessage>? messages, bool addRelevantDocuments, bool addFilesList, IToolsContext toolsContext, List<ImageData> images);
    Task<string> StreamResponse(string query, CancellationToken cancel, Action<string> onChunkReceived, IEnumerable<DevGPTChatMessage>? messages, bool addRelevantDocuments, bool addFilesList, IToolsContext toolsContext, List<ImageData> images);
    Task<ResponseType> GetResponse<ResponseType>(string query, CancellationToken cancel, IEnumerable<DevGPTChatMessage>? messages, bool addRelevantDocuments, bool addFilesList, IToolsContext toolsContext, List<ImageData> images) where ResponseType : ChatResponse<ResponseType>, new();
    Task<ResponseType> StreamResponse<ResponseType>(string query, CancellationToken cancel, Action<string> onChunkReceived, IEnumerable<DevGPTChatMessage>? messages, bool addRelevantDocuments, bool addFilesList, IToolsContext toolsContext, List<ImageData> images) where ResponseType : ChatResponse<ResponseType>, new();
    Task<string> UpdateStore(string query, CancellationToken cancel, IEnumerable<DevGPTChatMessage>? messages, bool addRelevantDocuments, bool addFilesList, IToolsContext toolsContext, List<ImageData> images);
    Task<string> StreamUpdateStore(string query, CancellationToken cancel, Action<string> onChunkReceived, IEnumerable<DevGPTChatMessage>? messages, bool addRelevantDocuments, bool addFilesList, IToolsContext toolsContext, List<ImageData> images);
}
