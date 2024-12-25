using OpenAI_API;
using OpenAI_API.Chat;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Forms;


namespace DevGPT.NewAPI
{
    public class GeneratorStore : BaseStore, IGeneratorStore
    {
        public Generator Generator { get; set; }
        public GeneratorStore(StoreConfig config) : base(config)
        {
            var openai = new OpenAIAPI(config.OpenAiApiKey);
            Generator = new Generator(openai);
        }

        public async Task<string> Generator_Question(string query)
        {
            var docsString = await RelevantDocumentsProvider.GetRelevantDocuments(query, Embeddings);
            return "";
        }

        public Task<string> Generator_UpdateStore(string query)
        {
            throw new NotImplementedException();
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
            ResponseMessage = "The response to the user",
        };
    }

    public class hoi
    {
        public static async void x()
        {
            var path = @"c:\projects\test";
            var embeddingsFilePath = @"c:\projects\test.embeddings.json";
            var c = new StoreConfig(path, embeddingsFilePath, Settings.OpenAIApiKey);
            var s = new GeneratorStore(c);
            s.Generator.SystemPrompt = "Converse with the user. Keep the store up to date by modifying documents with new information or deleting document that are no longer applicable.";
            s.ModifyDocument("Personal Info", "personal info.txt", "Name: Martien de Jong\nBirth date: 18-12-1983");

            Console.WriteLine("Start conversing");

            var messages = new List<ChatMessage>();
            while (true)
            {
                var m = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(m)) break;

                messages.Add(new ChatMessage(ChatMessageRole.User, m));

                var result = await s.Generator.GenerateObject<UpdateStoreResponse>(messages);
                result.Modifications.ForEach(m => s.ModifyDocument(m.Name, m.Path, m.Contents));
                result.Deletions.ForEach(m => s.RemoveDocument(m.Path));
                messages.Add(new ChatMessage(ChatMessageRole.Assistant, result.ResponseMessage));

                Console.WriteLine(result.ResponseMessage);
            }

        }
    }
}