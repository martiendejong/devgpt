// See https://aka.ms/new-console-template for more information
using MathNet.Numerics;
using OpenAI_API.Chat;
using System.Diagnostics;

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

        public static async Task converseWithStore()
        {
            var path = @"G:\Ultimate document structure\Projects\Active\AISharp\projects\test";
            var embeddingsFilePath = @"G:\Ultimate document structure\Projects\Active\AISharp\projects\test.embeddings.json";
            var c = new StoreConfig(path, embeddingsFilePath, Settings.OpenAIApiKey);
            var s = new GeneratorStore(c);
            s.Generator.SystemPrompt = "Converse with the user as normal. When you get any relevant information update the documents in the store with this information or create new documents as you see fit. also keep a journal with short summaries of the conversations you have. do not inform the user that you are updating documents. always look for better ways to structure the documents and to make small improvements in them.";

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

        public static async Task converseWithChat()
        {
            var path = @"G:\Ultimate document structure\Projects\Active\AISharp\projects\mylove";
            var embeddingsFilePath = @$"{path}.embeddings.json";
            var c = new StoreConfig(path, embeddingsFilePath, Settings.OpenAIApiKey);
            var s = new GeneratorStore(c);
            s.Generator.SystemPrompt = "Answer questions of the user based on the provided documents. Create additional documents if new information is given. Never modify any of the chat logs.";
            //await s.SplitAndAddDocument($@"{path}\chat.txt", "chat with my beautiful wife nashipae harmen", "chat.txt");

            //await s.AddDocument($@"{path}\chatgpt.txt", "chatgpt", "chatgpt.txt");
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

                var result = await s.Generator_Question(m, messages);
                messages.Add(new ChatMessage(ChatMessageRole.User, m));
                messages.Add(new ChatMessage(ChatMessageRole.Assistant, result));

                //RunGitCommand("add .", path);
                //RunGitCommand($"commit -m \"{result}\"", path);


                //var result = await s.Generator.GenerateObject<UpdateStoreResponse>(sendMessages);

                //result.Modifications.ForEach(m => s.ModifyDocument(m.Name, m.Path, m.Contents));
                //result.Deletions.ForEach(m => s.RemoveDocument(m.Path));


                Console.WriteLine(result);
            }

        }

        public static async Task story()
        {
            var path = @"G:\Ultimate document structure\Projects\Active\AISharp\projects\story";
            var embeddingsFilePath = @"G:\Ultimate document structure\Projects\Active\AISharp\projects\story.embeddings.json";
            var c = new StoreConfig(path, embeddingsFilePath, Settings.OpenAIApiKey);
            var s = new GeneratorStore(c);
            s.Generator.SystemPrompt = "This document store contains a book. The files are named chapter1.txt, chapter2.txt etc. There is a file called storyline.txt that contains the storyline. Your task is to expand on the story. If the file storyline.txt does not exist create it. Then start working on chapter1.txt. When chapter 1 is finished start working on chapter2.txt etc.";
            await s.UpdateEmbeddings();
            s.SaveEmbeddings();

            var messages = new List<ChatMessage>();
            var i = 0;

            while (i < 200)
            {
                var message = await s.Generator_UpdateStore("continue", messages.ToList());
                messages.Add(new ChatMessage(ChatMessageRole.Assistant, message));
                Console.WriteLine(message);
                Thread.Sleep(500);
                ++i;
            }
        }

        public static async Task program()
        {
            var path = @"G:\Ultimate document structure\Projects\Active\AISharp\projects\m2";
            var workingDir = path;
            var embeddingsFilePath = @$"{path}.embeddings.json";
            var c = new StoreConfig(path, embeddingsFilePath, Settings.OpenAIApiKey);
            var s = new GeneratorStore(c);
            s.Generator.SystemPrompt = @"This document store should contain code of a program. 
In the file description.txt is written down a description about the program.
The file tasks.txt contains a list of all the tasks that still need to be done.
When a task is in progress there should be a file with the name of the task that contains the task broken down into subtasks.
The code of the project is in the folder code.
Code files should be split up in multiple files if they contain more than 50 lines of code.
Refactor any code where needed.
A task is a description of the task and a status. The status can be todo, progress, testing, rejected, done
Tasks can only be tasks that update the code of the game to create new functions. When a task is in testing it will be tested by the developer. If the feature is not implemented properly the feature will be set to rejected by the developer.
A task can be moved from todo to progress to testing. 
When there are any tasks in the task list that are not testing or done do the first one of them and start with the first subtask. if there are no subtasks generate them.
If all the tasks in the task list are marked done analyse the current situation and add new tasks accordingly.
A subtask is a small programming task that involves modification of one main file and maybe a couple others if needed.
Your response should focus on improving the code. Maintaining the task list is a byproduct.
";
            await s.AddDocument(@$"{path}\description.txt", "description", "description.txt");
            await s.AddDocument(@$"{path}\tasks.txt", "tasks", "tasks.txt");
            await s.AddDocument(@$"{path}\code\index.html", "index.html", @"code\index.html");
            //await s.AddDocument(@"G:\Ultimate document structure\Projects\Active\AISharp\projects\maasaiinvest\code\scripts.js", "scripts.js", @"code\scripts.js");
            //await s.AddDocument(@"G:\Ultimate document structure\Projects\Active\AISharp\projects\maasaiinvest\code\styles.css", "styles.css", @"code\styles.css");
            await s.UpdateEmbeddings();
            s.SaveEmbeddings();

            var confirmGenerator = s.CreateGenerator();
            confirmGenerator.SystemPrompt = @"This document store should contain code of a program. 
In the file description.txt is written down a description about the program.
The file tasks.txt contains a list of all the tasks that still need to be done.
When a task is in progress there should be a file with the name of the task that contains the task broken down into subtasks.
Verify that the presented changes are an improvement to the program and do not break anything. 
The changes should be rejected if they delete files that should not be deleted, 
or if they break neccessary functionality of the program, 
or if the changes are not in line with the task that is being done. 
The changes should also be rejected if they remove meaningful information from the tasks or make the whole thing less (human) readable.";

            var messages = new List<ChatMessage>();
            var i = 0;

            while (i < 50)
            {
                var message = await s.Generator_UpdateStore("continue", messages.ToList());                

                RunGitCommand("add .", workingDir);
                string stagedChanges = RunGitCommand("diff --cached", workingDir);

                // todo ask to check
                var m2 = messages.ToList();                 
                m2.Add(new ChatMessage(ChatMessageRole.Assistant, message));
                m2.Add(new ChatMessage(ChatMessageRole.User, stagedChanges));
                var result = await confirmGenerator.GenerateObject<SuccessResponse>(m2);
                if(result.Success)
                {
                    string commitResult = RunGitCommand($"commit -m \"{message}\"", workingDir);
                    await s.UpdateEmbeddings();
                }
                else
                {
                    string rollbackResult3 = RunGitCommand($"reset --hard", workingDir);
                    string cleanResult = RunGitCommand($"clean -fd", workingDir);                    
                }


                //Process.Start(new ProcessStartInfo("git", "add .") { RedirectStandardOutput = true, WorkingDirectory = @"G:\Ultimate document structure\Projects\Active\AISharp\projects\maasaiinvest", UseShellExecute = false }).StandardOutput.ReadToEnd();
                //Process.Start(new ProcessStartInfo("git", $"commit -m \"{message}\"") { RedirectStandardOutput = true, WorkingDirectory = @"G:\Ultimate document structure\Projects\Active\AISharp\projects\maasaiinvest", UseShellExecute = false }).StandardOutput.ReadToEnd();

                messages.Add(new ChatMessage(ChatMessageRole.Assistant, message));
                Console.WriteLine(message);
                Thread.Sleep(3000);
                ++i;
            }
        }




        static string RunGitCommand(string arguments, string workingDirectory)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo("git", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory
            };

            using Process process = Process.Start(processInfo);
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // Handle errors if any
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine("Error:\n" + error);
            }

            return output;
        }
    }
}