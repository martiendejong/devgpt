using OpenAI;
using OpenAI.Chat;
using Store.OpnieuwOpnieuw;
using Store.OpnieuwOpnieuw.AIClient;
using Store.OpnieuwOpnieuw.DocumentStore;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace DevGPT.NewAPI
{
    public class DocumentGenerator : IDocumentGenerator
    {
        protected IDocumentStore Store { get; set; }
        protected List<IDocumentStore> ReadonlyStores { get; set; }
        //protected TypedOpenAIClient TypedApi { get; set; }
        //public SimpleOpenAIClient SimpleApi { get; set; }
        public List<DevGPTChatMessage> BaseMessages { get; set; }
        protected ILLMClient LLMClient { get; set; }

        public EmbeddingMatcher EmbeddingMatcher = new EmbeddingMatcher();

        public async Task<string> GetImage(string message, IEnumerable<DevGPTChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null, List<ImageData> images = null)
        {
            var response = await LLMClient.GetImage(message, DevGPTChatResponseFormat.Text, toolsContext, images);
            return response.Url;
        }

        public DocumentGenerator(DocumentStore store, List<DevGPTChatMessage> baseMessages, ILLMClient client, List<IDocumentStore> readonlyStores)
        {
            Store = store;
            BaseMessages = baseMessages;

            LLMClient = client;
            //TypedApi = new TypedOpenAIClient(OpenAIAPI, openAiApiKey, logger.Log);
            //SimpleApi = TypedApi;
            ReadonlyStores = readonlyStores;
        }

        public DocumentGenerator(DocumentStore store, List<DevGPTChatMessage> baseMessages, ILLMClient client, string openAiApiKey, string logFilePath, List<IDocumentStore> readonlyStores)
        {
            Store = store;
            ReadonlyStores = readonlyStores;
            BaseMessages = baseMessages;
            LLMClient = client;
            var OpenAIAPI = new OpenAIClient(openAiApiKey);
            //var logger = new Logger(logFilePath);
            //TypedApi = new TypedOpenAIClient(OpenAIAPI, openAiApiKey, logger.Log);
            //SimpleApi = TypedApi;
        }

        public async Task<string> GetResponse(string message, IEnumerable<DevGPTChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null, List<ImageData> images = null)
        {
            var sendMessages = await PrepareMessages(message, history, addRelevantDocuments, addFilesList);
            return await LLMClient.GetResponse(sendMessages.ToList(), DevGPTChatResponseFormat.Text, toolsContext, images);
        }

        public async Task<string> GetResponse(IEnumerable<DevGPTChatMessage> messages, IEnumerable<DevGPTChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null, List<ImageData> images = null)
        {
            var sendMessages = await PrepareMessages(messages.ToList(), history.ToList(), addRelevantDocuments, addFilesList);
            return await LLMClient.GetResponse(sendMessages, DevGPTChatResponseFormat.Text, toolsContext, images);
        }

        public async Task<string> StreamResponse(string message, Action<string> onChunkReceived, IEnumerable<DevGPTChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null, List<ImageData> images = null)
        {
            var sendMessages = await PrepareMessages(message, history, addRelevantDocuments, addFilesList);
            return await LLMClient.GetResponseStream(sendMessages, onChunkReceived, DevGPTChatResponseFormat.Text, toolsContext, images);
        }

        public async Task<string> StreamResponse(IEnumerable<DevGPTChatMessage> messages, Action<string> onChunkReceived, IEnumerable<DevGPTChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null, List<ImageData> images = null)
        {
            var sendMessages = await PrepareMessages(messages, history, addRelevantDocuments, addFilesList);
            return await LLMClient.GetResponseStream(sendMessages, onChunkReceived, DevGPTChatResponseFormat.Text, toolsContext, images);
        }

        public async Task<ResponseType> GetResponse<ResponseType>(string message, IEnumerable<DevGPTChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null, List<ImageData> images = null) where ResponseType : ChatResponse<ResponseType>, new()
        {
            var sendMessages = await PrepareMessages(message, history, addRelevantDocuments, addFilesList);
            return await LLMClient.GetResponse<ResponseType>(sendMessages, toolsContext, images);
        }

        public async Task<ResponseType> GetResponse<ResponseType>(IEnumerable<DevGPTChatMessage> messages, IEnumerable<DevGPTChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null, List<ImageData> images = null) where ResponseType : ChatResponse<ResponseType>, new()
        {
            var sendMessages = await PrepareMessages(messages, history, addRelevantDocuments, addFilesList);
            return await LLMClient.GetResponse<ResponseType>(sendMessages, toolsContext, images);
        }

        public async Task<ResponseType> StreamResponse<ResponseType>(string message, Action<string> onChunkReceived, IEnumerable<DevGPTChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null, List<ImageData> images = null) where ResponseType : ChatResponse<ResponseType>, new()
        {
            var sendMessages = await PrepareMessages(message, history, addRelevantDocuments, addFilesList);
            return await LLMClient.GetResponseStream<ResponseType>(sendMessages, onChunkReceived, toolsContext, images);
        }

        public async Task<ResponseType> StreamResponse<ResponseType>(IEnumerable<DevGPTChatMessage> messages, Action<string> onChunkReceived, IEnumerable<DevGPTChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null, List<ImageData> images = null) where ResponseType : ChatResponse<ResponseType>, new()
        {
            var sendMessages = await PrepareMessages(messages, history, addRelevantDocuments, addFilesList);
            return await LLMClient.GetResponseStream<ResponseType>(sendMessages, onChunkReceived, toolsContext, images);
        }

        public async Task<string> UpdateStore(string message, IEnumerable<DevGPTChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null, List<ImageData> images = null)
        {
            var sendMessages = await PrepareMessages(message, history, addRelevantDocuments, addFilesList);

            //var info = new DevGPTChatTool("writefile", "Writes content to a file and returns success or an error. Use this to modify files.", new List<ChatToolParameter>() {
            //        new ChatToolParameter { Name = "file", Description = "The relative path to the file being written", Type = "string" },
            //        new ChatToolParameter { Name = "content", Description = "The literal content that will be written to the file", Type = "string" }
            //    },
            //    async (messages, call) => {
            //        using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);
            //        var hasFile = argumentsJson.RootElement.TryGetProperty("file", out JsonElement file);
            //        if (!hasFile) return "file parameter not provided";
            //        var hasContent = argumentsJson.RootElement.TryGetProperty("content", out JsonElement content);
            //        if (!hasContent) return "content parameter not provided";
            //        try
            //        {
            //            await Store.Store(file.ToString(), content.ToString(), false);
            //            return "success";
            //        }
            //        catch (Exception ex)
            //        {
            //            return ex.Message;
            //        }
            //    }
            //);
            //toolsContext.Add(info);

            var response = await LLMClient.GetResponse<UpdateStoreResponse>(sendMessages, toolsContext, images);

            await ModifyDocuments(response);

            return response.ResponseMessage;
        }

        public async Task<string> UpdateStore(IEnumerable<DevGPTChatMessage> messages, IEnumerable<DevGPTChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null, List<ImageData> images = null)
        {
            var sendMessages = await PrepareMessages(messages, history, addRelevantDocuments, addFilesList);
            var response = await LLMClient.GetResponse<UpdateStoreResponse>(sendMessages, toolsContext, images);
            await ModifyDocuments(response);
            return response.ResponseMessage;
        }

        public async Task<string> StreamUpdateStore(string message, Action<string> onChunkReceived, IEnumerable<DevGPTChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null, List<ImageData> images = null)
        {
            var sendMessages = await PrepareMessages(message, history, addRelevantDocuments, addFilesList);
            var response = await LLMClient.GetResponseStream<UpdateStoreResponse>(sendMessages, onChunkReceived, toolsContext, images);
            await ModifyDocuments(response);
            return response.ResponseMessage;
        }

        private async Task ModifyDocuments(UpdateStoreResponse response)
        {
            if (response.Modifications != null)
                foreach (var modification in response.Modifications)
                {
                    await Store.Store(modification.Path, modification.Contents, false);
                    await Store.Store(modification.Path, modification.Contents, false);
                }
            if (response.Deletions != null)
                foreach (var deletion in response.Deletions)
                {
                    Store.Remove(deletion.Path);
                }
        }


        private async Task<List<DevGPTChatMessage>> PrepareMessages(string message, IEnumerable<DevGPTChatMessage>? messages, bool addRelevantDocuments, bool addFilesList)
            => await PrepareMessages([new DevGPTChatMessage { Role = DevGPTMessageRole.User, Text = message }], messages, addRelevantDocuments, addFilesList);

        private async Task<List<DevGPTChatMessage>> PrepareMessages(IEnumerable<DevGPTChatMessage> chatMessages, IEnumerable<DevGPTChatMessage>? history, bool addRelevantDocuments, bool addFilesList)
        {
            var sendMessages = history == null ? new List<DevGPTChatMessage>() : history.ToList();
            if (addRelevantDocuments)
            {
                var relevancyQuery = string.Join("\n\n", sendMessages.Concat(BaseMessages).Concat(chatMessages).Select(m => m.Role + ": " + m.Text));

                var embeddings = await Store.Embeddings(relevancyQuery);
                foreach(var s in ReadonlyStores)
                {
                    embeddings.AddRange(await s.Embeddings(relevancyQuery));
                }
                var e = new EmbeddingMatcher();
                var docs = await e.TakeTop(embeddings);

                var msgs = docs.Select(d => new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = d });

                sendMessages.AddRange(msgs);
            }
            if (addFilesList)
            {
                var filesList = Store.List();
                sendMessages.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = $"A list of all files in the document store:\n{filesList}" });
            }
            sendMessages.AddRange(BaseMessages);
            if (chatMessages.Any())
            {
                sendMessages.AddRange(chatMessages);
            }
            return sendMessages;
        }
    }
}