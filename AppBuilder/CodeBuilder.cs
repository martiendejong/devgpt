// See https://aka.ms/new-console-template for more information
using DevGPT.NewAPI;
using OpenAI.Chat;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

public class CodeBuilder2
{
    public Action<string> Output = (string a) => { };
    public string AppDir;
    public string OpenAiApiKey;
    public string LogFilePath;
    public ToolsContextBase CodingTools;

    public string DocumentStoreFolderPath;
    public string EmbeddingsFilePath;
    public DocumentStore Store;

    public DocumentStore TempStore;
    public string TempStoreFolderPath;
    public string TempStoreEmbeddingsFilePath;

    public List<CodeBuilderTask> Tasks = new List<CodeBuilderTask>();
    public CodeBuilder2(string appDir, string docStorePath, string embedPath, string openAoApiKey, string logFilePath, string tempStorePath, string tempStoreEmbeddingsFilePath)
    {
        AppDir = appDir;
        OpenAiApiKey = openAoApiKey;
        LogFilePath = logFilePath;

        DocumentStoreFolderPath = docStorePath;
        EmbeddingsFilePath = embedPath;
        var appFolderStoreConfig = new DocumentStoreConfig(DocumentStoreFolderPath, EmbeddingsFilePath, OpenAiApiKey);
        Store = new DocumentStore(appFolderStoreConfig);

        TempStoreFolderPath = tempStorePath;
        TempStoreEmbeddingsFilePath = tempStoreEmbeddingsFilePath;

        if (File.Exists(TempStoreEmbeddingsFilePath))
            File.Delete(TempStoreEmbeddingsFilePath);

        if (Directory.Exists(TempStoreFolderPath))
            File.Delete(TempStoreEmbeddingsFilePath);

        var tempStoreConfig = new DocumentStoreConfig(TempStoreFolderPath, TempStoreEmbeddingsFilePath, OpenAiApiKey);
        TempStore = new DocumentStore(tempStoreConfig);

        int level = 0;
        CodingTools = new ToolsContextBase();
        // tools: get task list, update task list, get elaborate specification
        CodingTools.Add("elaborate", "Asks the agent to generate an elaborate specification for an instruction. When you are asked to elaborate on something you are not allowed to call this function with the same instruction as that would cause a recursive loop.",
            new List<ChatToolParameter>() {
                new ChatToolParameter { Name = "instruction", Description = "The instruction that needs elaboration", Required = true, Type = "string" }
            },
            async (messages, call) =>
            {
                using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);
                if (argumentsJson.RootElement.TryGetProperty("instruction", out JsonElement question))
                {
                    level++;

                    if (level > 2) return "Elaboration is already two levels deep, you should have a detailed enough instruction by now.";

                    Console.WriteLine($"{level}.");

                    var response = await CreateSpecification(question.ToString());
                    Console.WriteLine($"{response.Item1}");

                    level--;

                    return response.Item1;
                }
                return "";
            }
        );
        CodingTools.Add("getfile", "Retrieves a document from the store.",
            new List<ChatToolParameter>() {
                new ChatToolParameter { Name = "path", Description = "The store relative path to the file.", Required = true, Type = "string" }
            },
            async (messages, call) =>
            {
                using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);
                if (argumentsJson.RootElement.TryGetProperty("path", out JsonElement question))
                {
                    try
                    {
                        var path = Store.GetFilePath(question.ToString());
                        var text = File.ReadAllText(path);
                        return text;
                    }
                    catch (Exception ex)
                    {
                        return ex.Message;
                    }
                }
                return "path not provided";
            }
        );
        CodingTools.Add("getfileslist", "Gets the store files list.", new List<ChatToolParameter>(), async (messages, call) =>
        {
            Console.WriteLine($"Get files list");
            var result = Store.GetFilesList();
            Console.WriteLine(result);
            return result;
        });
        CodingTools.Add("gettaskslist", "Gets the current task list.", new List<ChatToolParameter>(), async (messages, call) =>
        {
            Console.WriteLine($"Get task list");
            var result = JsonSerializer.Serialize(Tasks);
            Console.WriteLine(result);            
            return result;
        });
        CodingTools.Add("updatetask", "Updates the task with the given title.",
            new List<ChatToolParameter>() {
                new ChatToolParameter { Name = "title", Description = "The title of the task to update", Required = true, Type = "string" },
                new ChatToolParameter { Name = "description", Description = "The updated description", Required = false, Type = "string" },
                new ChatToolParameter { Name = "status", Description = "The updated status. Allowed values: todo test or done", Required = false, Type = "string" }
            },
            async (messages, call) =>
            {
                using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);
                var hasTitle = argumentsJson.RootElement.TryGetProperty("title", out JsonElement title);
                var hasDescription = argumentsJson.RootElement.TryGetProperty("description", out JsonElement description);
                var hasStatus = argumentsJson.RootElement.TryGetProperty("status", out JsonElement status);

                if (hasTitle)
                {
                    var descriptionVal = hasDescription ? description.ToString() :  null;
                    var statusVal = hasStatus ? status.ToString() : null;

                    var response = UpdateTask(title.ToString(), descriptionVal, statusVal);
                    return response ? "SUCCESS" : "FAILED, did you provide the right title?";
                }
                return "FAILED, no title provided";
            }
        );
        CodingTools.Add("createtask", "Creates a new task.",
            new List<ChatToolParameter>() {
                new ChatToolParameter { Name = "title", Description = "The title of the task", Required = true, Type = "string" },
                new ChatToolParameter { Name = "description", Description = "The description of the task", Required = true, Type = "string" },
            },
            async (messages, call) =>
            {
                using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);
                var hasTitle = argumentsJson.RootElement.TryGetProperty("title", out JsonElement title);
                var hasDescription = argumentsJson.RootElement.TryGetProperty("description", out JsonElement description);

                if (hasTitle && hasDescription)
                {
                    Tasks.Add(new CodeBuilderTask() { Title = title.ToString(), Description = description.ToString(), Status = "todo" });
                    return "SUCCESS";
                }
                return "FAILED, no title or no descrption provided";
            }
        );

        //CodingTools.Add("AskAgent", "Asks an agent a question and return the response.",
        //    new List<ChatToolParameter>() {
        //        new ChatToolParameter { Name = "question", Description = "The question to ask the agent", Required = true, Type = "string" }
        //    },
        //    async (messages, call) =>
        //    {
        //        using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);
        //        if (argumentsJson.RootElement.TryGetProperty("question", out JsonElement question))
        //        {
        //            level++;
        //            Console.WriteLine($"{level}. Ask agent: {question.ToString()}");

        //            var generator = new DocumentGenerator(Store, [new SystemChatMessage("Answer the question of the agent.")], OpenAiApiKey, new Logger(logFilePath));
        //            var response = await generator.GetResponse(question.ToString(), messages, true, true, CodingTools);

        //            level--;

        //            return response;
        //        }
        //        return "";
        //    }
        //);
        //CodingTools.Add("Build", "Builds the Quasar app and returns the build output.", new List<ChatToolParameter>(), async (messages, call) =>
        //{
        //    var output = QuasarBuildOutput.GetQuasarBuildOutput(AppDir);
        //    return output.Item2;
        //});
        CodingTools.Add("Git", "Calls git and returns the output.",
            new List<ChatToolParameter>() {
                new ChatToolParameter { Name = "arguments", Description = "The arguments to call git with", Required = true, Type = "string" }
            },
            async (messages, call) =>
            {
                using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);
                if (argumentsJson.RootElement.TryGetProperty("arguments", out JsonElement args))
                {
                    var output = GitOutput.GetGitOutput(docStorePath, args.ToString());
                    return output.Item1 + "\n" + output.Item2;
                }
                return "arguments not provided";
            }
        );

    }

    public async Task AddFiles(string[] fileFilters, string subDirectory = "", string[] excludePattern = null)
    {
        var dir = subDirectory == "" ? new DirectoryInfo(DocumentStoreFolderPath) : new DirectoryInfo(Path.Combine(DocumentStoreFolderPath, subDirectory));

        var filesParts = new List<FileInfo[]>();
        foreach (var item in fileFilters)
        {
            filesParts.Add(dir.GetFiles(item, SearchOption.AllDirectories));
        }
        var files = filesParts.SelectMany(f => f).ToList();

        foreach (var file in files)
        {
            var relPath = file.FullName.Substring((DocumentStoreFolderPath + "\\").Length);
            if (excludePattern == null || !excludePattern.Any(dir => MatchPattern(relPath, dir)))
            {
                await Store.AddDocument(file.FullName, file.Name, relPath, false);
            }
        }

        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();
    }

    public bool MatchPattern(string text, string pattern)
    {
        if(pattern.StartsWith("*"))
            return text.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase);
        return text.Contains(pattern, StringComparison.InvariantCultureIgnoreCase);
    }

    public List<ChatMessage> History = new List<ChatMessage>();

    public async Task Execute(string instruction)
    {
        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();

        History.Add(instruction);

        var feedback = instruction;
        var specificationPath = "";
        var specification = "";
        while (feedback.Trim() != "")
        {
            if (specification == "")
            {
                var t = await CreateSpecification(feedback);
                specification = t.Item1;
                specificationPath = t.Item2;
            }
            else
                specification = await UpdateSpecification(feedback, specificationPath);

            Console.WriteLine("Give feedback on the specification");
            feedback = Console.ReadLine();
        }

        Tasks = await CreateTasks(specification);

        while (Tasks.Any(t => t.Status.ToLower() != "done"))
        {
            var work = Tasks.Where(t => t.Status.ToLower() != "done").ToList();
            var task = work.OrderBy(t => t.Status.ToLower() == "test" ? 0 : 1).First();
            if(task.Status.ToLower() == "test")
                await VerifyTask(task);
            else
                await ExecuteTask(task);
        }

        ////await Start(instruction);

        ///*
        // * 1. Elaborate on the instruction to create an elaborate specification
        // * 2. Store the specification in a temporary document embeddings cache
        // * 3. Create one or more tasks based on the specification, keep the task list in the temporary document embeddings cache
        // * 4. For each of the tasks
        // *      1. Execute the task, with tools 'elaborate on the instruction'
        // */





        //var success = false;
        //while (!success)
        //{
        //    Output("Analyze instruction");
        //    var specification = await CreateSpecification(instruction);

        //    Output("Create tasks");
        //    var tasks = await CreateTasks(specification);

        //    Output("Execute tasks");
        //    await ExecuteTasks(tasks);

        //    Output("Verifying");
        //    var totalTask = new CodeBuilderTask { Title = instruction, Description = specification };
        //    success = await VerifyTotalTask(totalTask);
        //}

        //await Finish(instruction);
    }

    private async Task ExecuteTasks(List<CodeBuilderTask> tasks)
    {
        foreach (var task in tasks)
        {
            LogAddMessage(task.Title);
            await ExecuteTask(task);

            Output("Verifying");
            var success = await VerifyTask(task);
            while (!success)
            {
                LogAddMessage(task.Title);
                await ExecuteTask(task);

                Output("Verifying");
                success = await VerifyTask(task);
            }
        }
    }

    private async Task Start(string instruction)
    {
        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();

        // Get current branch name
        var branchResult = GitOutput.GetGitOutput(DocumentStoreFolderPath, "rev-parse --abbrev-ref HEAD");
        string branch = branchResult.Item1.Trim();

        // Get current commit hash
        var commitResult = GitOutput.GetGitOutput(DocumentStoreFolderPath, "rev-parse HEAD");
        string commit = commitResult.Item1.Trim();

        var promptExecute = @$"You are an expert software engineer. You will use Git Flow to commit and test your changes. You are now in branch {branch} at commit {commit}. You have been given the task {instruction}. See if a branch exists for this feature that is reasonably uptodate. If so, checkout that branch. If the branch does not exist yet create it based on develop and check it out.";
        var generatorExecute = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptExecute) }, OpenAiApiKey, LogFilePath, new List<IStore> { TempStore });

        var messages = new List<ChatMessage> { new UserChatMessage("analyze the git branches and checkout a recent existing branch or create and checkout a new feature branch.") };
        var message = await generatorExecute.GetResponse<CodeBuilderContinuous>(messages, History, true, true, CodingTools);
        messages.Add(new AssistantChatMessage(message.Message));
        LogAddMessage(message.Message);
        var finished = message.Finished;
        while (!finished)
        {
            message = await generatorExecute.GetResponse<CodeBuilderContinuous>(messages, History, true, true, CodingTools);
            LogAddMessage(message.Message);
            finished = message.Finished;
        }

        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();
    }

    private async Task Finish(string instruction)
    {
        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();

        // Get current branch name
        var branchResult = GitOutput.GetGitOutput(DocumentStoreFolderPath, "rev-parse --abbrev-ref HEAD");
        string branch = branchResult.Item1.Trim();

        // Get current commit hash
        var commitResult = GitOutput.GetGitOutput(DocumentStoreFolderPath, "rev-parse HEAD");
        string commit = commitResult.Item1.Trim();

        var promptExecute = @$"You are an expert software engineer. You will use Git Flow to commit and test your changes. You are now in branch {branch} at commit {commit}. The task {instruction} has been finished. Now commit your code and create a pull request with the develop branch. Also switch to the develop branch. Do not merge the branch with develop yourself.";
        var generatorExecute = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptExecute) }, OpenAiApiKey, LogFilePath, new List<IStore> { TempStore });

        var messages = new List<ChatMessage> { new UserChatMessage("commit your code and create a pull request with develop. switch to develop. do not merge the branch with develop yourself, just make the pull request.") };
        var message = await generatorExecute.GetResponse<CodeBuilderContinuous>(messages, History, true, true, CodingTools);
        messages.Add(new AssistantChatMessage(message.Message));
        LogAddMessage(message.Message);
        var finished = message.Finished;
        while (!finished)
        {
            message = await generatorExecute.GetResponse<CodeBuilderContinuous>(messages, History, true, true, CodingTools);
            LogAddMessage(message.Message);
            finished = message.Finished;
        }

        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();
    }

    private async Task ExecuteTask(CodeBuilderTask task)
    {
        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();

        var instruction = $"{task.Title}\n{task.Description}";

        //var specification = await CreateSpecification(instruction);

        var promptExecute = @$"You are an expert software engineer. Execute the task as specified. Create or update files where needed. If there is a problem with the existing code always look into the git history to get an understanding of the situation.";
        var generatorExecute = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptExecute) }, OpenAiApiKey, LogFilePath, new List<IStore> { TempStore });

        var message = await generatorExecute.UpdateStore(instruction, History, true, true, CodingTools);
        LogAddMessage(message);

        task.Status = "test";

        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();
        TempStore.SaveEmbeddings();
    }

    private void LogAddMessage(string instruction)
    {
        History.Add(instruction);
        Output(instruction);
    }

    private async Task<bool> VerifyTask(CodeBuilderTask task)
    {
        var instruction = $"{task.Title}\n{task.Description}";

        var promptVerify = @"Analyze the updated code and verify that the task is complete. Carefully analyze the locally changed files and compare them with the previous version to make sure the changes are proper and make sense. Make sure all the updated code is right and that it generates no errors. If there is anything that can be improved show the rework. If the changes are problematic discard them. If the code is completely right commit it with git.";
        var generatorVerify = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptVerify) }, OpenAiApiKey, LogFilePath, new List<IStore> { TempStore });
        var message = await generatorVerify.GetResponse<CodeBuilderVerify>(instruction, History, true, true, CodingTools);

        if (message.HasRework)
        {
            task.Description += $"\nRework:\n{message.Rework}";
            task.Status = "todo";
            LogAddMessage(message.Rework);
            return false;
        }
        task.Status = "done";
        return true;
    }

    private async Task<bool> VerifyTotalTask(CodeBuilderTask task)
    {
        var instruction = $"{task.Title}\n{task.Description}";

        var promptVerify = @"Analyze the updated code and verify that the task is complete. Make sure all the updated code is right and that it generates no errors. Compare the branch with develop and analyse the differences. If there is anything that can be improved show the rework. If the code is completely right create a pull request to merge it with develop.";
        var generatorVerify = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptVerify) }, OpenAiApiKey, LogFilePath, new List<IStore> { TempStore });
        var message = await generatorVerify.GetResponse<CodeBuilderVerify>(instruction, History, true, true, CodingTools);

        if (message.HasRework)
        {
            task.Description += $"\nRework:\n{message.Rework}";
            LogAddMessage(message.Rework);
            return false;
        }
        return true;
    }

    private async Task<Tuple<string, string>> CreateSpecification(string instruction)
    {
        var fileTitle = $"Elaborate on instruction: {instruction}";
        Console.WriteLine(fileTitle);

        var promptAnalayzeInstruction = @"Analyze the instruction from the user. Analyze relevant documents. Translate the instruction of the user to a complete specification for the developer AI agent. When relevant analyse the git history or build output. Specify explicitly which files likely need to be changed to accomplish this task. Also specify assumptions and restraints. Specify suggestions about architecture and approach. Make it conrete. Clearly state the goal of the plan. Try to use a maximum of 200 words, preferrably shorter but if you absolutely need more words then create a longer specification. Leave out testing, that is something the user will do.";
        var generatorAnalayzeInstruction = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptAnalayzeInstruction) }, OpenAiApiKey, LogFilePath, new List<IStore> { TempStore });
        var specification = await generatorAnalayzeInstruction.GetResponse(instruction, History, true, true, CodingTools);

        var path = Guid.NewGuid().ToString();
        await TempStore.ModifyDocument(fileTitle, path, specification);
        TempStore.SaveEmbeddings();

        Console.WriteLine(specification);

        return new Tuple<string, string> ( specification, path);
    }

    private async Task<string> UpdateSpecification(string feedback, string guid)
    {
        var e = TempStore.GetEmbeddings().First(e => e.Path == guid);
        var d = TempStore.GetFilePath(e.Path);
        var content = File.ReadAllText(d);

        var promptAnalayzeInstruction = @"Analyze the feedback from the user. Analyze relevant documents. Update the specification for the developer AI agent. When relevant analyse the git history or build output. Specify explicitly which files likely need to be changed to accomplish this task. Also specify assumptions and restraints. Specify suggestions about architecture and approach. Make it conrete. Clearly state the goal of the plan. Try to user around 200 words but if you absolutely need more words then create a longer specification.";
        var generatorAnalayzeInstruction = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptAnalayzeInstruction) }, OpenAiApiKey, LogFilePath, new List<IStore> { TempStore });

        var m = new List<ChatMessage>();
        m.Add(new SystemChatMessage(e.Name));
        m.Add(new SystemChatMessage(content));
        m.Add(new UserChatMessage(feedback));

        var specification = await generatorAnalayzeInstruction.GetResponse(m, History, true, true, CodingTools);

        await TempStore.ModifyDocument(e.Name, e.Path, specification);
        TempStore.SaveEmbeddings();

        Console.WriteLine(specification);

        return specification;
    }

    private async Task<List<CodeBuilderTask>> CreateTasks(string instruction)
    {
        var promptAnalayzeInstruction = @"Create a list consisting of the task or tasks needed to implement changes to satisfy the specification. Tasks consist of small code changes in one file or a couple of files. Be aware that the token limit constrains the number of files you can edit. Specify per task explicitly which files likely need to be changed to accomplish this task. Also specify assumptions and restraints. Specify suggestions about architecture and approach. Try to keep each task description at a maximum of around 100 words but make sure the description is always complete and clear without any ambiguity.";
        var generatorAnalayzeInstruction = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptAnalayzeInstruction) }, OpenAiApiKey, LogFilePath, new List<IStore> { TempStore });
        var response = await generatorAnalayzeInstruction.GetResponse<CodeBuilderTasks>(instruction, History, true, true);
        // todo store in tasks file (not needed?)
        var tasks = response.Tasks;
        Console.WriteLine(string.Join("\n", tasks.Select(t => $"{t.Title}\n{t.Description}\n")));

        return tasks;
    }

    private bool UpdateTask(string currentTitle, string? description, string? status)
    {
        var task = Tasks.FirstOrDefault(t => t.Title.ToLower() == currentTitle.ToLower());
        if (task == null) return false;
        if(description != null)
            task.Description = description;
        if (status != null)
            task.Status = status;
        return true;
    }
}
