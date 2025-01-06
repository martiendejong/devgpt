using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Moderation;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;


namespace DevGPT.NewAPI
{
    public class GeneratorStore : BaseStore, IGeneratorStore
    {
        public Generator Generator { get; set; }
        public GeneratorStore(StoreConfig config) : base(config)
        {
            Generator = CreateGenerator();
        }

        public Generator CreateGenerator()
        {
            var openai = new OpenAIAPI(Config.OpenAiApiKey);
            return new Generator(openai);
        }


        public async Task<string> Generator_Question(string m, IEnumerable<ChatMessage>? messages = null)
        {
            var sendMessages = messages == null ? new List<ChatMessage>() : messages.ToList();

            var docsString = GetRelevantDocuments(m);
            //var docsString = await s.RelevantDocumentsProvider.GetRelevantDocuments(m, s.Embeddings);
            var filesList = string.Join("\n", Embeddings.Select(e => $"{e.Path}"));
            var query = $"{m}\n\nDocuments:\n{docsString}\n\nFiles:\n{filesList}";
            sendMessages.Add(new ChatMessage(ChatMessageRole.User, query));

            return await Generator.Generate(sendMessages);
        }

        public async Task<string> GetRelevantDocuments(string query)
        {
            return await RelevantDocumentsProvider.GetRelevantDocuments(query, Embeddings);
        }

        public async Task<string> Generator_UpdateStore(string m, IEnumerable<ChatMessage>? messages = null)
        {
            var sendMessages = messages == null ? new List<ChatMessage>() : messages.ToList();

            var docsString = await GetRelevantDocuments(m);
            //var docsString = await s.RelevantDocumentsProvider.GetRelevantDocuments(m, s.Embeddings);
            var filesList = string.Join("\n", Embeddings.Select(e => $"{e.Path}"));
            var query = $"{m}\n\nFiles:\n{filesList}\n\nDocuments:\n{docsString}";

            sendMessages.Add(new ChatMessage(ChatMessageRole.User, query));

            var response = await Generator.GenerateObject<UpdateStoreResponse>(sendMessages);

            if(response.Modifications != null)
                foreach(var modification in response.Modifications)
                {
                    await ModifyDocument(modification.Name, modification.Path, modification.Contents);
                }
            if (response.Deletions != null)
                foreach (var deletion in response.Deletions)
                {
                    await RemoveDocument(deletion.Path);
                }
            SaveEmbeddings();

            return response.ResponseMessage;
        }
    }

    public class DeleteDocumentRequest
    {
        public string Path { get; set; }
    }

    public class ModifyDocumentRequest
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Contents { get; set; }
    }

    public class UpdateStoreResponse : ChatResponse<UpdateStoreResponse>
    {
        public List<ModifyDocumentRequest> Modifications { get; set; }
        public List<DeleteDocumentRequest> Deletions { get; set; }
        public string ResponseMessage { get; set; }

        public override UpdateStoreResponse _example => new UpdateStoreResponse()
        {
            Modifications = new List<ModifyDocumentRequest>() { new ModifyDocumentRequest { Name = "Name of the document, ie. My Personal File", Path = "The relative path, ie. info\\personalfile.txt", Contents = "The (updated) contents of the file." } },
            Deletions = new List<DeleteDocumentRequest>() { new DeleteDocumentRequest { Path = "The relative path, ie. info\\olddocument.txt" } },
            ResponseMessage = "The response to the user"
        };

        public override string _signature => @"{Modifications: [] or null, Deletions: [] or null, ResponseMessage: string}";
    }
}