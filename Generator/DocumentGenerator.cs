using OpenAI;
using OpenAI.Chat;
using System.Collections.Generic;

//using OpenAI.Moderation;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;


namespace DevGPT.NewAPI
{
    public class DocumentGenerator : IDocumentGenerator
    {
        protected IStore Store { get; set; }
        protected TypedOpenAIClient TypedApi { get; set; }
        public SimpleOpenAIClient SimpleApi { get; set; }
        protected List<ChatMessage> BaseMessages { get; set; }
        protected OpenAIClient OpenAIAPI { get; set; }

        public DocumentGenerator(DocumentStore store, List<ChatMessage> baseMessages, string openAiApiKey, ILogger logger)
        {
            Store = store;
            BaseMessages = baseMessages;

            OpenAIAPI = new OpenAIClient(openAiApiKey);
            //var logger = new Logger(logFilePath);
            TypedApi = new TypedOpenAIClient(OpenAIAPI, openAiApiKey, logger.Log);
            SimpleApi = TypedApi;
        }

        public DocumentGenerator(DocumentStore store, List<ChatMessage> baseMessages, string openAiApiKey, string logFilePath)
        {
            Store = store;
            BaseMessages = baseMessages;

            var OpenAIAPI = new OpenAIClient(openAiApiKey);
            var logger = new Logger(logFilePath);
            TypedApi = new TypedOpenAIClient(OpenAIAPI, openAiApiKey, logger.Log);
            SimpleApi = TypedApi;
        }

        public async Task<string> GetResponse(string message, IEnumerable<ChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null)
        {
            var sendMessages = await PrepareMessages(message, history, addRelevantDocuments, addFilesList);
            return await SimpleApi.GetResponse(sendMessages, ChatResponseFormat.CreateTextFormat(), toolsContext);
        }

        public async Task<string> StreamResponse(string message, Action<string> onChunkReceived, IEnumerable<ChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null)
        {
            var sendMessages = await PrepareMessages(message, history, addRelevantDocuments, addFilesList);
            return await SimpleApi.GetResponseStream(sendMessages, onChunkReceived, ChatResponseFormat.CreateTextFormat(), toolsContext);
        }

        public async Task<string> StreamResponse(IEnumerable<ChatMessage> messages, Action<string> onChunkReceived, IEnumerable<ChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null)
        {
            var sendMessages = await PrepareMessages(messages, history, addRelevantDocuments, addFilesList);
            return await SimpleApi.GetResponseStream(sendMessages, onChunkReceived, ChatResponseFormat.CreateTextFormat(), toolsContext);
        }

        public async Task<ResponseType> GetResponse<ResponseType>(string message, IEnumerable<ChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null) where ResponseType : ChatResponse<ResponseType>, new()
        {
            var sendMessages = await PrepareMessages(message, history, addRelevantDocuments, addFilesList);
            return await TypedApi.GetResponse<ResponseType>(sendMessages, toolsContext);
        }

        public async Task<ResponseType> StreamResponse<ResponseType>(string message, Action<string> onChunkReceived, IEnumerable<ChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null) where ResponseType : ChatResponse<ResponseType>, new()
        {
            var sendMessages = await PrepareMessages(message, history, addRelevantDocuments, addFilesList);
            return await TypedApi.GetResponseStream<ResponseType>(sendMessages, onChunkReceived, toolsContext);
        }

        public async Task<ResponseType> StreamResponse<ResponseType>(IEnumerable<ChatMessage> messages, Action<string> onChunkReceived, IEnumerable<ChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null) where ResponseType : ChatResponse<ResponseType>, new()
        {
            var sendMessages = await PrepareMessages(messages, history, addRelevantDocuments, addFilesList);
            return await TypedApi.GetResponseStream<ResponseType>(sendMessages, onChunkReceived, toolsContext);
        }

        public async Task<string> UpdateStore(string message, IEnumerable<ChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null)
        {
            var sendMessages = await PrepareMessages(message, history, addRelevantDocuments, addFilesList);
            var response = await TypedApi.GetResponse<UpdateStoreResponse>(sendMessages, toolsContext);
            await ModifyDocuments(response);
            return response.ResponseMessage;
        }

        public async Task<string> StreamUpdateStore(string message, Action<string> onChunkReceived, IEnumerable<ChatMessage>? history = null, bool addRelevantDocuments = true, bool addFilesList = true, IToolsContext toolsContext = null)
        {
            var sendMessages = await PrepareMessages(message, history, addRelevantDocuments, addFilesList);
            var response = await TypedApi.GetResponseStream<UpdateStoreResponse>(sendMessages, onChunkReceived, toolsContext);
            await ModifyDocuments(response);
            return response.ResponseMessage;
        }

        private async Task ModifyDocuments(UpdateStoreResponse response)
        {
            if (response.Modifications != null)
                foreach (var modification in response.Modifications)
                {
                    await Store.ModifyDocument(modification.Name, modification.Path, modification.Contents);
                }
            if (response.Deletions != null)
                foreach (var deletion in response.Deletions)
                {
                    await Store.RemoveDocument(deletion.Path);
                }
            Store.SaveEmbeddings();
        }

        private async Task<List<ChatMessage>> PrepareMessages(string message, IEnumerable<ChatMessage>? messages, bool addRelevantDocuments, bool addFilesList)
        {
            var sendMessages = messages == null ? new List<ChatMessage>() : messages.ToList();
            if (addRelevantDocuments)
            {
                var relevancyQuery = string.Join("\n\n", sendMessages.Concat(BaseMessages).Select(m => Logger.GetMessageType(m) + ": " + m.Content.First().Text));
                relevancyQuery += "\n\nuser: " + message;
                var msgs = await Store.GetRelevantDocumentsAsChatMessages(relevancyQuery);
                //var mainRelevantDoc = docs.First();

                //var msgs = await Store.GetRelevantDocumentsAsChatMessages(relevancyQuery + "\n\n" + mainRelevantDoc);

                sendMessages.AddRange(msgs);
            }
            if (addFilesList)
            {
                var filesList = Store.GetFilesList();
                sendMessages.Add(new AssistantChatMessage($"A list of all files in the document store:\n{filesList}"));
            }
            sendMessages.AddRange(BaseMessages);
            if(!string.IsNullOrWhiteSpace(message))
                sendMessages.Add(new UserChatMessage(message));
            return sendMessages;
        }

        private async Task<List<ChatMessage>> PrepareMessages(IEnumerable<ChatMessage> chatMessages, IEnumerable<ChatMessage>? history, bool addRelevantDocuments, bool addFilesList)
        {
            var sendMessages = history == null ? new List<ChatMessage>() : history.ToList();
            if (addRelevantDocuments)
            {
                var relevancyQuery = string.Join("\n\n", sendMessages.Concat(BaseMessages).Select(m => Logger.GetMessageType(m) + ": " + m.Content.First().Text));
                foreach(var message in chatMessages)
                {
                    relevancyQuery += "\n\n" + Logger.GetMessageType(message) + ": " + message.Content.First().Text;
                }
                var relevantDocumentMessages = await Store.GetRelevantDocumentsAsChatMessages(relevancyQuery);
                //var mainRelevantDoc = docs.First();

                //var msgs = await Store.GetRelevantDocumentsAsChatMessages(relevancyQuery + "\n\n" + mainRelevantDoc);

                sendMessages.AddRange(relevantDocumentMessages);
            }
            if (addFilesList)
            {
                var filesList = Store.GetFilesList();
                sendMessages.Add(new AssistantChatMessage($"A list of all files in the document store:\n{filesList}"));
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