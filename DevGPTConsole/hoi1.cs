// See https://aka.ms/new-console-template for more information
using OpenAI_API.Chat;

namespace DevGPT.NewAPI
{
    public class hoi
    {
        public static async Task beheerportaal_controllers()
        {
            var path = @"c:\projects\beheerportaal\webservice\controllers";
            var embeddingsFilePath = @"c:\projects\bp.ws.controllers.embeddings.json";
            var c = new StoreConfig(path, embeddingsFilePath, Settings.OpenAIApiKey);
            var s = new GeneratorStore(c);

            s.Generator.SystemPrompt = "Converse with the user. Keep the store up to date by modifying documents with new information or deleting document that are no longer applicable.";

            await AddFiles(path, s, "*.cs");
            s.SaveEmbeddings();



            Console.WriteLine("Start conversing");

            var messages = new List<ChatMessage>();
            while (true)
            {
                var m = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(m)) break;

                var docsString = await s.RelevantDocumentsProvider.GetRelevantDocuments(m, s.Embeddings);

                var filesList = string.Join("\n", s.Embeddings.Select(e => $"{e.Name}:{e.Path}"));

                var query = $"{m}\n\nDocuments:\n{docsString}\n\nFiles:\n{filesList}";

                var sendMessages = messages.ToList();

                messages.Add(new ChatMessage(ChatMessageRole.User, m));
                sendMessages.Add(new ChatMessage(ChatMessageRole.User, query));


                var result = await s.Generator.GenerateObject<UpdateStoreResponse>(sendMessages);
                result.Modifications.ForEach(m => s.ModifyDocument(m.Name, m.Path, m.Contents));
                result.Deletions.ForEach(m => s.RemoveDocument(m.Path));
                messages.Add(new ChatMessage(ChatMessageRole.Assistant, result.ResponseMessage));

                Console.WriteLine(result.ResponseMessage);
            }

        }

        private static async Task<bool> AddFiles(string path, GeneratorStore s, string ext)
        {
            var files = Directory.GetFiles(path, ext, new EnumerationOptions() { RecurseSubdirectories = true });
            var remove = new DirectoryInfo(path).FullName;

            foreach (var file in files)
            {
                var relPath = file.Substring(remove.Length);
                var contents = File.ReadAllText(file);
                await s.UpdateEmbedding(new FileInfo(file).Name, relPath);
                //await s.ModifyDocument(new FileInfo(file).Name, relPath, contents);
            }

            return true;
        }


        private static async Task<bool> AddFiles(string path, GeneratorStore s, IEnumerable<string> files)
        {
            var remove = new DirectoryInfo(path).FullName;

            foreach (var file in files)
            {
                try
                {
                    await s.UpdateEmbedding(new FileInfo(file).Name, file);
                    s.SaveEmbeddings();
                }
                catch(Exception ex) 
                {
                    Console.WriteLine($"Embedding failed for file {file}");
                    Console.WriteLine(ex.ToString());
                }
            }

            return true;
        }

        public static async Task x()
        {
            var path = @"c:\projects\test";
            var embeddingsFilePath = @"c:\projects\test.embeddings.json";
            var c = new StoreConfig(path, embeddingsFilePath, Settings.OpenAIApiKey);
            var s = new GeneratorStore(c);
            s.Generator.SystemPrompt = "Converse with the user as normal. When you get any relevant information update the documents in the store with this information or create new documents as you see fit. also keep a journal with short summaries of the conversations you have. do not inform the user that you are updating documents.";

            //await AddFiles(path, s, "*.*");
            //s.SaveEmbeddings();

            //s.SaveEmbeddings();


            Console.WriteLine("Start conversing");


            //s.UpdateEmbedding("Kenya house plan", "/investment/kenya_house_plan.txt");
            //s.SaveEmbeddings();
            var messages = new List<ChatMessage>();
            while (true)
            {
                var m = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(m)) break;

                //var sendMessages = messages.ToList();

                var result = await s.Generator_UpdateStore(m, messages);
                messages.Add(new ChatMessage(ChatMessageRole.User, m));
                messages.Add(new ChatMessage(ChatMessageRole.Assistant, result));


                //var result = await s.Generator.GenerateObject<UpdateStoreResponse>(sendMessages);

                //result.Modifications.ForEach(m => s.ModifyDocument(m.Name, m.Path, m.Contents));
                //result.Deletions.ForEach(m => s.RemoveDocument(m.Path));


                Console.WriteLine(result);
            }

        }



        public static async Task gitrepo()
        {
            var path = @"c:\projects\beheerportaal";
            var embeddingsFilePath = @"c:\projects\beheerportaal.embeddings.v2.json";
            var c = new StoreConfig(path, embeddingsFilePath, Settings.OpenAIApiKey);
            var s = new GeneratorStore(c);

            

            //var files = GitFileSelector.GetMatchingFiles(path, ["*", "!*.csr", "!*.docx", "!*.exe", "!*.pem", "!*.jpg", "!*.png", "!*.mp4", "!*.svg", "!*.ico", "!*.drawio"]);
            //await AddFiles(path, s, files);
            //s.SaveEmbeddings();



            s.Generator.SystemPrompt = "Converse with the user as normal. Based on the conversation and the supplied documents update the code by modifying existing files or creating new ones.";



            Console.WriteLine("Start conversing");


            var messages = new List<ChatMessage>();
            while (true)
            {
                var m = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(m)) break;

                //var sendMessages = messages.ToList();

                var result = await s.Generator_UpdateStore(m, messages);
                messages.Add(new ChatMessage(ChatMessageRole.User, m));
                messages.Add(new ChatMessage(ChatMessageRole.Assistant, result));


                //var result = await s.Generator.GenerateObject<UpdateStoreResponse>(sendMessages);

                //result.Modifications.ForEach(m => s.ModifyDocument(m.Name, m.Path, m.Contents));
                //result.Deletions.ForEach(m => s.RemoveDocument(m.Path));


                Console.WriteLine(result);
            }

        }
    }
}