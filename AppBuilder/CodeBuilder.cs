// See https://aka.ms/new-console-template for more information
using DevGPT.NewAPI;
using OpenAI.Chat;
using Store.OpnieuwOpnieuw;
using Store.OpnieuwOpnieuw.AIClient;
using Store.OpnieuwOpnieuw.DocumentStore;
using System.Reflection.Emit;
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
    public ToolsContextBase SpecificationTools;
    public ToolsContextBase ReflectionTools;
    public ToolsContextBase ProjectManagerInitialTools;
    public ToolsContextBase ProjectManagerTools;
    public ToolsContextBase CoderTools { get
        {
            string[] coderToolNames = ["getfileslist", "getfile", "gettaskslist", "updatetask", "createtask", "git"];
            var coderTools = new ToolsContextBase();
            ChatTools.Where(t => coderToolNames.Contains(t.FunctionName)).ToList().ForEach(coderTools.Add);
            return coderTools;
        }
    }
    public ToolsContextBase ReviewerTools {  get {
            string[] reviewerToolNames = ["gettaskslist", "updatetask", "createtask", "git", "build"];
            var reviewerTools = new ToolsContextBase();
            ChatTools.Where(t => reviewerToolNames.Contains(t.FunctionName)).ToList().ForEach(t => reviewerTools.Add(t));
            return reviewerTools;
        }
    }
    public List<DevGPTChatTool> ChatTools;

    public string DocumentStoreFolderPath;
    public string EmbeddingsFilePath;
    public DocumentStore Store;

    public DocumentStore TempStore;
    public string TempStoreFolderPath;
    public string TempStoreEmbeddingsFilePath;

    public List<CodeBuilderTask> Tasks = new List<CodeBuilderTask>();

    int level = 0;

    //public static string CoderPrompt = @$"You are an expert software engineer. Execute the task as specified. Create or update files where needed. When your task contains rework make sure to use git to analyze history. When you see things that need to be done that fall out of the scope of this task create a new task for it. If needed update existing tasks with information. ALWAYS PROVIDE THE WHOLE FILE THAT YOU ARE MODIFYING, NEVER LEAVE ANYTHING OUT OR WRITE // ... (rest of the methods remain unchanged) ";
    public static string CoderPrompt = File.ReadAllText("CodePrompt.txt");// @$"You are an expert software engineer. Execute the task as specified. Create or update files where needed. When your task contains rework make sure to use git to analyze history. When you see things that need to be done that fall out of the scope of this task create a new task for it. If needed update existing tasks with information. ALWAYS PROVIDE THE WHOLE FILE THAT YOU ARE MODIFYING, NEVER LEAVE ANYTHING OUT OR WRITE // ... (rest of the methods remain unchanged) ";
    public static string ReviewerPrompt = File.ReadAllText("ReviewPrompt.txt");//@"Your goal is to review the given task. Use git to carefully analyze the locally changed files and compare them with the previous version to make sure the changes are proper and make sense, that no neccessary code is deleted and that existing functionality remains working. Use the build function to make sure all the updated code is working correctly and that it generates no errors. If there is anything that can be improved show the rework. When you see things that need to be done that fall out of the scope of this task create a new task for it. If the changes are problematic discard them. If the code is completely right commit it with git.";

    public async Task<string> Elaborate(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
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

    public async Task<string> Reflect(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);
        if (argumentsJson.RootElement.TryGetProperty("statement", out JsonElement question))
        {
            level++;

            if (level > 2) return "Reflection is already two levels deep, you should have a detailed enough instruction by now.";

            Console.WriteLine($"{level}.");

            var response = await Reflect(question.ToString());
            Console.WriteLine($"{response.Item1}");

            level--;

            return response.Item1;
        }
        return "";
    }

    public async Task<string> GetFile(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);
        if (argumentsJson.RootElement.TryGetProperty("path", out JsonElement question))
        {
            try
            {
                Console.WriteLine($"Get file {question.ToString()}");
                var text = await Store.Get(question.ToString());
                //var path = Store.GetFilePath(question.ToString());
                //var text = File.ReadAllText(path);
                return text;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        return "path not provided";
    }

    public async Task<string> GetFilesList(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        Console.WriteLine($"Get files list");
        var result = Store.List();
        var msg = string.Join("\n", result);
        Console.WriteLine(msg);
        return msg;
    }

    public async Task<string> GetTasksList(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        Console.WriteLine($"Get task list");
        var result = JsonSerializer.Serialize(Tasks);
        Console.WriteLine(result);
        return result;
    }

    public async Task<string> UpdateTask(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);
        var hasTitle = argumentsJson.RootElement.TryGetProperty("title", out JsonElement title);
        var hasDescription = argumentsJson.RootElement.TryGetProperty("description", out JsonElement description);
        var hasStatus = argumentsJson.RootElement.TryGetProperty("status", out JsonElement status);

        if (hasTitle)
        {
            Console.WriteLine($"Updating task {title.ToString()}");
            Console.WriteLine($"{description.ToString()}");

            var descriptionVal = hasDescription ? description.ToString() : null;
            var statusVal = hasStatus ? status.ToString() : null;

            var response = UpdateTask(title.ToString(), descriptionVal, statusVal);
            return response ? "SUCCESS" : "FAILED, did you provide the right title?";
        }
        return "FAILED, no title provided";
    }

    public async Task<string> CreateTask(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);
        var hasTitle = argumentsJson.RootElement.TryGetProperty("title", out JsonElement title);
        var hasDescription = argumentsJson.RootElement.TryGetProperty("description", out JsonElement description);

        if (hasTitle && hasDescription)
        {
            Console.WriteLine($"Create task {title.ToString()}");
            Console.WriteLine($"{description.ToString()}");

            Tasks.Add(new CodeBuilderTask() { Title = title.ToString(), Description = description.ToString(), Status = "todo" });
            return "SUCCESS";
        }
        return "FAILED, no title or no descrption provided";
    }

    public async Task<string> Git(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);
        if (argumentsJson.RootElement.TryGetProperty("arguments", out JsonElement args))
        {
            Console.WriteLine($"Calling git {args.ToString()}");

            var output = GitOutput.GetGitOutput(DocumentStoreFolderPath, args.ToString());
            return output.Item1 + "\n" + output.Item2;
        }
        return "arguments not provided";
    }

    public async Task<string> Build(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        Console.WriteLine($"Building the project");

        var output = BuildOutput.GetBuildOutput(DocumentStoreFolderPath, "build.bat", "build_errors.log");
        Console.WriteLine($"Building the project");
        return output;
    }


    public CodeBuilder2(string appDir, string docStorePath, string embedPath, string partsFilePath, string openAoApiKey, string logFilePath, string tempStorePath, string tempStoreEmbeddingsFilePath, string tempStorePartsFilePath)
    {
        AppDir = appDir;
        OpenAiApiKey = openAoApiKey;
        LogFilePath = logFilePath;

        DocumentStoreFolderPath = docStorePath;
        EmbeddingsFilePath = embedPath;

        var openAIConfig = new OpenAIConfig(OpenAiApiKey);
        var llmClient = new OpenAIClientWrapper(openAIConfig);
        var embeddingStore = new EmbeddingFileStore(EmbeddingsFilePath, llmClient);
        var textStore = new TextFileStore(DocumentStoreFolderPath);
        var partStore = new DocumentPartFileStore(partsFilePath);
        
        Store = new DocumentStore(embeddingStore, textStore, partStore);


        TempStoreFolderPath = tempStorePath;
        TempStoreEmbeddingsFilePath = tempStoreEmbeddingsFilePath;

        if (File.Exists(TempStoreEmbeddingsFilePath))
            File.Delete(TempStoreEmbeddingsFilePath);

        if (Directory.Exists(TempStoreFolderPath))
            File.Delete(TempStoreEmbeddingsFilePath);

        var tempEmbeddingStore = new EmbeddingFileStore(TempStoreEmbeddingsFilePath, llmClient);
        var tempTextStore = new TextFileStore(TempStoreFolderPath);
        var tempPartStore = new DocumentPartFileStore(tempStorePartsFilePath);

        //var tempStoreConfig = new DocumentStoreConfig(TempStoreFolderPath, TempStoreEmbeddingsFilePath, OpenAiApiKey);
        TempStore = new DocumentStore(tempEmbeddingStore, tempTextStore, tempPartStore);
        
        ChatTools =
        [
            new DevGPTChatTool("reflect",
                "Asks the reasoning assistant to reflect on a statement.",
                [
                    new() { Name = "statement", Description = "The statement that will be evaluated.", Required = true, Type = "string" }
                ], Reflect
            ),
            new DevGPTChatTool("elaborate", 
                "Asks the agent to generate an elaborate specification for an instruction. When you are asked to elaborate on something you are not allowed to call this function with the same instruction as that would cause a recursive loop.",
                [
                    new() { Name = "instruction", Description = "The instruction that needs elaboration", Required = true, Type = "string" }
                ], Elaborate
            ),
            new DevGPTChatTool("getfile", "Retrieves a document from the store.",
            [
                new() { Name = "path", Description = "The store relative path to the file.", Required = true, Type = "string" }
            ],
            GetFile
                ),
            new DevGPTChatTool("getfileslist", "Gets the store files list.", [], GetFilesList
                ),
            new DevGPTChatTool("gettaskslist", "Gets the current task list.", [], GetTasksList
                ),
            new DevGPTChatTool("updatetask", "Updates the task with the given title.",
            [
                new() { Name = "title", Description = "The title of the task to update", Required = true, Type = "string" },
                new() { Name = "description", Description = "The updated description", Required = false, Type = "string" },
                new() { Name = "status", Description = "The updated status. Allowed values: todo test or done", Required = false, Type = "string" }
            ], UpdateTask
                ),
            new DevGPTChatTool("createtask", "Creates a new task.",
            [
                new() { Name = "title", Description = "The title of the task", Required = true, Type = "string" },
                new() { Name = "description", Description = "The description of the task", Required = true, Type = "string" },
            ],
            CreateTask
                ),
            new DevGPTChatTool("git", "Calls git and returns the output.",
            [
                new() { Name = "arguments", Description = "The arguments to call git with", Required = true, Type = "string" }
            ], Git
                ),
            new DevGPTChatTool("build", "Builds the solution and returns the output.",
            [], Build
                ),
        ];

        string[] specificationToolNames = ["getfile", "getfileslist", "git", "reflect"];
        SpecificationTools = new ToolsContextBase();
        ChatTools.Where(t => specificationToolNames.Contains(t.FunctionName)).ToList().ForEach(t => SpecificationTools.Add(t));

        string[] reflectionToolNames = ["getfile", "getfileslist", "git"];
        ReflectionTools = new ToolsContextBase();
        ChatTools.Where(t => reflectionToolNames.Contains(t.FunctionName)).ToList().ForEach(t => ReflectionTools.Add(t));


        string[] projectManagerIToolNames = ["reflect"];
        ProjectManagerInitialTools = new ToolsContextBase();
        ChatTools.Where(t => projectManagerIToolNames.Contains(t.FunctionName)).ToList().ForEach(t => ProjectManagerInitialTools.Add(t));


        string[] projectManagerToolNames = ["gettaskslist", "updatetask", "createtask", "reflect"];
        ProjectManagerTools = new ToolsContextBase();
        ChatTools.Where(t => projectManagerToolNames.Contains(t.FunctionName)).ToList().ForEach(t => ProjectManagerTools.Add(t));


        //string[] coderPrepareToolNames = ["getfileslist", "getfile", "gettaskslist", "updatetask", "createtask", "git"];
        //CoderTools = new ToolsContextBase();
        //ToolInfos.Where(t => coderToolNames.Contains(t.Name)).ToList().ForEach(t => CoderTools.Add(t));





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
                //var content = await File.ReadAllTextAsync(file.FullName);
                await Store.Embed(relPath);
            }
        }
    }

    public bool MatchPattern(string text, string pattern)
    {
        if(pattern.StartsWith("*"))
            return text.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase);
        return text.Contains(pattern, StringComparison.InvariantCultureIgnoreCase);
    }

    public List<DevGPTChatMessage> History = new List<DevGPTChatMessage>();

    public async Task Execute2(string instruction)
    {
        //History.Add(instruction);

        // create an agent that is an operative who calls other agents to solve the problems. it will partially complete tasks and keep responding to the user as well.
        //
        // it has the following agents that it can call:
        // promptanalaysis(prompt) analyzes the prompt and comes up with a description of what the user wants
        // gitanalysis(prompt) analyzes the repository to try and answer the question
        // gitanalysis(prompt) analyzes the repository to try and answer the question

    }

    public async Task Execute(string instruction)
    {
        History.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.User, Text = instruction });

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
            if (task.Status.ToLower() == "test")
            {
                try
                {
                    await ReviewTask(task);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            else
            {
                try
                {
                    await ExecuteTask(task);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
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
            var success = await ReviewTask(task);
            while (!success)
            {
                LogAddMessage(task.Title);
                await ExecuteTask(task);

                Output("Verifying");
                success = await ReviewTask(task);
            }
        }
    }

    private async Task Start(string instruction)
    {
        // Get current branch name
        var branchResult = GitOutput.GetGitOutput(DocumentStoreFolderPath, "rev-parse --abbrev-ref HEAD");
        string branch = branchResult.Item1.Trim();

        // Get current commit hash
        var commitResult = GitOutput.GetGitOutput(DocumentStoreFolderPath, "rev-parse HEAD");
        string commit = commitResult.Item1.Trim();

        var config = new OpenAIConfig(OpenAiApiKey);
        var llmClient = new OpenAIClientWrapper(config);

        var promptExecute = @$"You are an expert software engineer. You will use Git Flow to commit and test your changes. You are now in branch {branch} at commit {commit}. You have been given the task {instruction}. See if a branch exists for this feature that is reasonably uptodate. If so, checkout that branch. If the branch does not exist yet create it based on develop and check it out.";
        var generatorExecute = new DocumentGenerator(Store, new List<DevGPTChatMessage>() { new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = promptExecute } }, llmClient, OpenAiApiKey, LogFilePath, new List<IDocumentStore> { TempStore });

        var messages = new List<DevGPTChatMessage> { new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = "analyze the git branches and checkout a recent existing branch or create and checkout a new feature branch." } };
        var message = await generatorExecute.GetResponse<CodeBuilderContinuous>(messages, History, true, true, SpecificationTools);
        messages.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = message.Message });
        LogAddMessage(message.Message);
        var finished = message.Finished;
        while (!finished)
        {
            message = await generatorExecute.GetResponse<CodeBuilderContinuous>(messages, History, true, true, SpecificationTools);
            LogAddMessage(message.Message);
            finished = message.Finished;
        }
    }

    private async Task Finish(string instruction)
    {
        // Get current branch name
        var branchResult = GitOutput.GetGitOutput(DocumentStoreFolderPath, "rev-parse --abbrev-ref HEAD");
        string branch = branchResult.Item1.Trim();

        // Get current commit hash
        var commitResult = GitOutput.GetGitOutput(DocumentStoreFolderPath, "rev-parse HEAD");
        string commit = commitResult.Item1.Trim();

        var config = new OpenAIConfig(OpenAiApiKey);
        var llmClient = new OpenAIClientWrapper(config);

        var promptExecute = @$"You are an expert software engineer. You will use Git Flow to commit and test your changes. You are now in branch {branch} at commit {commit}. The task {instruction} has been finished. Now commit your code and create a pull request with the develop branch. Also switch to the develop branch. Do not merge the branch with develop yourself.";
        var generatorExecute = new DocumentGenerator(Store, new List<DevGPTChatMessage>() { new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = promptExecute } }, llmClient, OpenAiApiKey, LogFilePath, new List<IDocumentStore> { TempStore });

        var messages = new List<DevGPTChatMessage> { new DevGPTChatMessage { Role = DevGPTMessageRole.User, Text = "commit your code and create a pull request with develop. switch to develop. do not merge the branch with develop yourself, just make the pull request." } };
        var message = await generatorExecute.GetResponse<CodeBuilderContinuous>(messages, History, true, true, SpecificationTools);
        messages.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = message.Message });
        LogAddMessage(message.Message);
        var finished = message.Finished;
        while (!finished)
        {
            message = await generatorExecute.GetResponse<CodeBuilderContinuous>(messages, History, true, true, SpecificationTools);
            LogAddMessage(message.Message);
            finished = message.Finished;
        }
    }

    private async Task ExecuteTask(CodeBuilderTask task)
    {
        var instruction = $"{task.Title}\n{task.Description}";

        //var specification = await CreateSpecification(instruction);

        var config = new OpenAIConfig(OpenAiApiKey);
        var llmClient = new OpenAIClientWrapper(config);


        var promptExecute = CoderPrompt;
        var generatorExecute = new DocumentGenerator(Store, new List<DevGPTChatMessage>() { new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = promptExecute } }, llmClient, OpenAiApiKey, LogFilePath, new List<IDocumentStore> { TempStore });

        var message = await generatorExecute.UpdateStore(instruction, [] /*History*/, true, true, CoderTools);
        LogAddMessage(message);

        task.Status = "test";
    }

    private void LogAddMessage(string instruction)
    {
        History.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.User, Text = instruction });
        Output(instruction);
    }

    private async Task<bool> ReviewTask(CodeBuilderTask task)
    {
        var instruction = $"{task.Title}\n{task.Description}";

        var promptVerify = ReviewerPrompt;

        var config = new OpenAIConfig(OpenAiApiKey);
        var llmClient = new OpenAIClientWrapper(config);

        var generatorVerify = new DocumentGenerator(Store, new List<DevGPTChatMessage>() { new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = promptVerify } }, llmClient, OpenAiApiKey, LogFilePath, new List<IDocumentStore> { TempStore });
        var message = await generatorVerify.GetResponse<CodeBuilderVerify>(instruction, [] /*History*/, true, true, ReviewerTools);

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

        var config = new OpenAIConfig(OpenAiApiKey);
        var llmClient = new OpenAIClientWrapper(config);


        var promptVerify = @"Analyze the updated code and verify that the task is complete. Make sure all the updated code is right and that it generates no errors. Compare the branch with develop and analyse the differences. If there is anything that can be improved show the rework. If the code is completely right create a pull request to merge it with develop.";
        var generatorVerify = new DocumentGenerator(Store, new List<DevGPTChatMessage>() { new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = promptVerify } }, llmClient, OpenAiApiKey, LogFilePath, new List<IDocumentStore> { TempStore });
        var message = await generatorVerify.GetResponse<CodeBuilderVerify>(instruction, History, true, true, SpecificationTools);

        if (message.HasRework)
        {
            task.Description += $"\nRework:\n{message.Rework}";
            LogAddMessage(message.Rework);
            return false;
        }
        return true;
    }

    private async Task<Tuple<string, string>> Reflect(string statement)
    {
        // this agent is supposed to generate an extensive specification.
        // to do this it should be able to consult the code and git
        // it should also be able to consult 


        var fileTitle = $"Reflect on the statement: {statement}";
        Console.WriteLine(fileTitle);

        var config = new OpenAIConfig(OpenAiApiKey);
        var llmClient = new OpenAIClientWrapper(config);

        var promptAnalayzeInstruction =
            @"Reflect on the given statement. Write down your chain of thought. Your goal is to provide the asker with a proper evaluation of the statement.";
        var generatorAnalayzeInstruction = new DocumentGenerator(Store, new List<DevGPTChatMessage>() { new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = promptAnalayzeInstruction } }, llmClient, OpenAiApiKey, LogFilePath, new List<IDocumentStore> { TempStore });
        var response = await generatorAnalayzeInstruction.GetResponse(statement, History, true, true, ReflectionTools);

        var path = Guid.NewGuid().ToString();
        await TempStore.Store(path, response, false);

        Console.WriteLine(response);

        return new Tuple<string, string>(response, path);
    }



    private async Task<Tuple<string, string>> CreateSpecification(string instruction)
    {
        // this agent is supposed to generate an extensive specification.
        // to do this it should be able to consult the code and git
        // it should also be able to consult 

        var config = new OpenAIConfig(OpenAiApiKey);
        var llmClient = new OpenAIClientWrapper(config);

        var fileTitle = $"Elaborate on instruction: {instruction}";
        Console.WriteLine(fileTitle);

        var promptAnalayzeInstruction = 
            @"Analyze the instruction from the user. Analyze relevant documents. Validate your resoning with the reasoning assistant. Translate the instruction of the user to a complete specification for the developer AI agent. When relevant analyse the git history or build output. Specify explicitly which files likely need to be changed to accomplish this task. Also specify assumptions and restraints. Specify suggestions about architecture and approach. Make it conrete. Clearly state the goal of the plan. Try to use a maximum of 200 words, preferrably shorter but if you absolutely need more words then create a longer specification.";
        var generatorAnalayzeInstruction = new DocumentGenerator(Store, new List<DevGPTChatMessage>() { new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = promptAnalayzeInstruction } }, llmClient, OpenAiApiKey, LogFilePath, new List<IDocumentStore> { TempStore });
        var specification = await generatorAnalayzeInstruction.GetResponse(instruction, History, true, true, SpecificationTools);

        var path = Guid.NewGuid().ToString();
        await TempStore.Store(path, specification, false);

        Console.WriteLine(specification);

        return new Tuple<string, string> ( specification, path);
    }

    private async Task<string> UpdateSpecification(string feedback, string guid)
    {
        var e = await TempStore.Get(guid);

        var config = new OpenAIConfig(OpenAiApiKey);
        var llmClient = new OpenAIClientWrapper(config);

        var promptAnalayzeInstruction = @"Analyze the feedback from the user. Analyze relevant documents. Update the specification for the developer AI agent. When relevant analyse the git history or build output. Specify explicitly which files likely need to be changed to accomplish this task. Also specify assumptions and restraints. Specify suggestions about architecture and approach. Make it conrete. Clearly state the goal of the plan. Try to user around 200 words but if you absolutely need more words then create a longer specification.";
        var generatorAnalayzeInstruction = new DocumentGenerator(Store, new List<DevGPTChatMessage>() { new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = promptAnalayzeInstruction } }, llmClient, OpenAiApiKey, LogFilePath, new List<IDocumentStore> { TempStore });

        var m = new List<DevGPTChatMessage>();
        m.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = e });
        m.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.User, Text = feedback });

        var specification = await generatorAnalayzeInstruction.GetResponse(m, History, true, true, SpecificationTools);

        await TempStore.Store(guid, specification, false);

        Console.WriteLine(specification);

        return specification;
    }

    private async Task<List<CodeBuilderTask>> CreateTasks(string instruction)
    {
        var config = new OpenAIConfig(OpenAiApiKey);
        var llmClient = new OpenAIClientWrapper(config);

        var promptAnalayzeInstruction = @"Create a list of tasks for making the code changes to implement the specification. A task consists of a code change that implements part of the specification. Only have tasks that have actual code changes, unless there is something really complex that needs to be worked out that spans over multiple files and involves creation of new tasks. if there are multiple trivial changes in one file combine them in one task. leave out any tasks for testing or review as that is handled separately by another agent.";
        var generatorAnalayzeInstruction = new DocumentGenerator(Store, new List<DevGPTChatMessage>() { new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = promptAnalayzeInstruction } }, llmClient, OpenAiApiKey, LogFilePath, new List<IDocumentStore> { TempStore });
        var response = await generatorAnalayzeInstruction.GetResponse<CodeBuilderTasks>(instruction, History, true, true, ProjectManagerInitialTools);
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
