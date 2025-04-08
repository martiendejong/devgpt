// See https://aka.ms/new-console-template for more information
using DevGPT.NewAPI;
using OpenAI.Chat;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

public class CodeBuilder
{
    public Action<string> Output = (string a) => { };
    public string AppDir;
    public string DocumentStoreFolderPath;
    public string EmbeddingsFilePath;
    public string OpenAiApiKey;
    public string LogFilePath;
    public DocumentStore Store;
    public ToolsContextBase CodingTools;
    public CodeBuilder(string appDir, string docStorePath, string embedPath, string openAoApiKey, string logFilePath)
    {
        AppDir = appDir;
        DocumentStoreFolderPath = docStorePath;
        EmbeddingsFilePath = embedPath;
        OpenAiApiKey = openAoApiKey;
        LogFilePath = logFilePath;

        var appFolderStoreConfig = new DocumentStoreConfig(DocumentStoreFolderPath, EmbeddingsFilePath, OpenAiApiKey);
        Store = new DocumentStore(appFolderStoreConfig);

        CodingTools = new ToolsContextBase();
        CodingTools.Add("Build", "Builds the Quasar app and returns the build output.", new List<ChatToolParameter>(), async (messages, call) =>
        {
            var output = QuasarBuildOutput.GetQuasarBuildOutput(AppDir);
            return output.Item2;
        });
        CodingTools.Add("Git", "Calls git and returns the output.", 
            new List<ChatToolParameter>() { 
                new ChatToolParameter { Name = "arguments", Description = "The arguments to call git with", Required = true, Type = "string" }
            }, 
            async (messages, call) =>
            {
                using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);
                if(argumentsJson.RootElement.TryGetProperty("arguments", out JsonElement args))
                {
                    var output = GitOutput.GetGitOutput(docStorePath, args.ToString());
                    return output.Item1 + "\n" + output.Item2;
                }
                return "";
            }
        );

    }

    public async Task AddFiles(string[] fileFilters)
    {
        var dir = new DirectoryInfo(DocumentStoreFolderPath);

        var filesParts = new List<FileInfo[]>();
        foreach (var item in fileFilters)
        {
            filesParts.Add(dir.GetFiles(item, SearchOption.AllDirectories));
        }
        var files = filesParts.SelectMany(f => f).ToList();

        foreach (var file in files)
        {
            var relPath = file.FullName.Substring((DocumentStoreFolderPath + "\\").Length);
            await Store.AddDocument(file.FullName, file.Name, relPath, false);
        }

        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();
    }

    public List<ChatMessage> History = new List<ChatMessage>();

    public async Task Build(string instruction)
    {
        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();

        Start(instruction);
        History.Add(instruction);

        var success = false;
        while (!success) {

            Output("Analyze input");
            var specification = await CreateSpecification(instruction);

            Output("Create tasks");
            var tasks = await CreateTasks(specification);

            Output("Execute tasks");
            await ExecuteTasks(tasks);

            Output("Verifying");
            var totalTask = new CodeBuilderTask { Title = instruction, Description = specification };
            success = await VerifyTask(totalTask);
        }

        await Finish(instruction);
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

        var promptExecute = @$"You are an expert software engineer. You will use Git Flow to commit and test your changes. You are now in branch {branch} at commit {commit}. You have been given the task {instruction}. Checkout a new branch based on develop so you can start coding.";
        var generatorExecute = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptExecute) }, OpenAiApiKey, LogFilePath);
        var message = await generatorExecute.UpdateStore("commit your code and merge your branch with main and switch to main.", History, true, true, CodingTools);

        LogAddMessage(message);

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

        var promptExecute = @$"You are an expert software engineer. You will use Git Flow to commit and test your changes. You are now in branch {branch} at commit {commit}. The task {instruction} has been finished. Now commit your code and merge your branch with main and switch to main.";
        var generatorExecute = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptExecute) }, OpenAiApiKey, LogFilePath);
        var message = await generatorExecute.UpdateStore("commit your code and merge your branch with main and switch to main.", History, true, true, CodingTools);

        LogAddMessage(message);

        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();
    }

    private async Task ExecuteTask(CodeBuilderTask task)
    {
        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();

        var instruction = $"{task.Title}\n{task.Description}";

        var specification = await CreateSpecification(instruction);
        
        var promptExecute = @$"You are an expert software engineer. Execute the task as specified.";
        var generatorExecute = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptExecute) }, OpenAiApiKey, LogFilePath);
        var message = await generatorExecute.UpdateStore(specification, History, true, true, CodingTools);

        LogAddMessage(message);

        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();
    }

    private void LogAddMessage(string instruction)
    {
        History.Add(instruction);
        Output(instruction);
    }

    private async Task<bool> VerifyTask(CodeBuilderTask task)
    {
        var instruction = $"{task.Title}\n{task.Description}";

        var specification = await CreateSpecification(instruction);

        var promptVerify = @"Verify that the task is copmlete. If not show me the rework.";
        var generatorVerify = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptVerify) }, OpenAiApiKey, LogFilePath);
        var message = await generatorVerify.GetResponse<CodeBuilderVerify>(instruction, History, true, true, CodingTools);

        if (message.HasRework)
        {
            task.Description += $"\nRework:\n{message.Rework}";
            LogAddMessage(message.Rework);
            return false;
        }
        return true;
    }

    private async Task<string> CreateSpecification(string instruction)
    {
        var promptAnalayzeInstruction = @"Analyze the instruction from the user. Analyze relevant documents. Translate the instruction of the user to a complete specification.";
        var generatorAnalayzeInstruction = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptAnalayzeInstruction) }, OpenAiApiKey, LogFilePath);
        var specification = await generatorAnalayzeInstruction.GetResponse(instruction, History, true, true, CodingTools);
        return specification;
    }

    private async Task<List<CodeBuilderTask>> CreateTasks(string instruction)
    {
        var promptAnalayzeInstruction = @"Create a list of tasks based on the specification by the user.";
        var generatorAnalayzeInstruction = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptAnalayzeInstruction) }, OpenAiApiKey, LogFilePath);
        var response = await generatorAnalayzeInstruction.GetResponse<CodeBuilderTasks>(instruction, History, true, true);
        // todo store in tasks file
        var tasks = response.Tasks;

        return tasks;
    }
}

public class CodeBuilder2
{
    public Action<string> Output = (string a) => { };
    public string AppDir;
    public string DocumentStoreFolderPath;
    public string EmbeddingsFilePath;
    public string OpenAiApiKey;
    public string LogFilePath;
    public DocumentStore Store;
    public ToolsContextBase CodingTools;
    public CodeBuilder2(string appDir, string docStorePath, string embedPath, string openAoApiKey, string logFilePath)
    {
        AppDir = appDir;
        DocumentStoreFolderPath = docStorePath;
        EmbeddingsFilePath = embedPath;
        OpenAiApiKey = openAoApiKey;
        LogFilePath = logFilePath;

        var appFolderStoreConfig = new DocumentStoreConfig(DocumentStoreFolderPath, EmbeddingsFilePath, OpenAiApiKey);
        Store = new DocumentStore(appFolderStoreConfig);

        int level = 0;
        CodingTools = new ToolsContextBase();
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
        CodingTools.Add("Build", "Builds the Quasar app and returns the build output.", new List<ChatToolParameter>(), async (messages, call) =>
        {
            var output = QuasarBuildOutput.GetQuasarBuildOutput(AppDir);
            return output.Item2;
        });
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
                return "";
            }
        );

    }

    public async Task AddFiles(string[] fileFilters)
    {
        var dir = new DirectoryInfo(DocumentStoreFolderPath);

        var filesParts = new List<FileInfo[]>();
        foreach (var item in fileFilters)
        {
            filesParts.Add(dir.GetFiles(item, SearchOption.AllDirectories));
        }
        var files = filesParts.SelectMany(f => f).ToList();

        foreach (var file in files)
        {
            var relPath = file.FullName.Substring((DocumentStoreFolderPath + "\\").Length);
            await Store.AddDocument(file.FullName, file.Name, relPath, false);
        }

        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();
    }

    public List<ChatMessage> History = new List<ChatMessage>();

    public async Task Build(string instruction)
    {
        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();

        History.Add(instruction);
        await Start(instruction);

        var success = false;
        while (!success)
        {
            Output("Analyze instruction");
            var specification = await CreateSpecification(instruction);

            Output("Create tasks");
            var tasks = await CreateTasks(specification);

            Output("Execute tasks");
            await ExecuteTasks(tasks);

            Output("Verifying");
            var totalTask = new CodeBuilderTask { Title = instruction, Description = specification };
            success = await VerifyTotalTask(totalTask);
        }

        await Finish(instruction);
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
        var generatorExecute = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptExecute) }, OpenAiApiKey, LogFilePath);

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
        var generatorExecute = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptExecute) }, OpenAiApiKey, LogFilePath);

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
        var generatorExecute = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptExecute) }, OpenAiApiKey, LogFilePath);
              
        var message = await generatorExecute.UpdateStore(instruction, History, true, true, CodingTools);
        LogAddMessage(message);

        await Store.UpdateEmbeddings();
        Store.SaveEmbeddings();
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
        var generatorVerify = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptVerify) }, OpenAiApiKey, LogFilePath);
        var message = await generatorVerify.GetResponse<CodeBuilderVerify>(instruction, History, true, true, CodingTools);

        if (message.HasRework)
        {
            task.Description += $"\nRework:\n{message.Rework}";
            LogAddMessage(message.Rework);
            return false;
        }
        return true;
    }

    private async Task<bool> VerifyTotalTask(CodeBuilderTask task)
    {
        var instruction = $"{task.Title}\n{task.Description}";

        var promptVerify = @"Analyze the updated code and verify that the task is complete. Make sure all the updated code is right and that it generates no errors. Compare the branch with develop and analyse the differences. If there is anything that can be improved show the rework. If the code is completely right create a pull request to merge it with develop.";
        var generatorVerify = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptVerify) }, OpenAiApiKey, LogFilePath);
        var message = await generatorVerify.GetResponse<CodeBuilderVerify>(instruction, History, true, true, CodingTools);

        if (message.HasRework)
        {
            task.Description += $"\nRework:\n{message.Rework}";
            LogAddMessage(message.Rework);
            return false;
        }
        return true;
    }

    private async Task<string> CreateSpecification(string instruction)
    {
        var promptAnalayzeInstruction = @"Analyze the instruction from the user. Analyze relevant documents. Translate the instruction of the user to a complete specification. When relevant analyse the git history or build output.";
        var generatorAnalayzeInstruction = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptAnalayzeInstruction) }, OpenAiApiKey, LogFilePath);
        var specification = await generatorAnalayzeInstruction.GetResponse(instruction, History, true, true, CodingTools);
        return specification;
    }

    private async Task<List<CodeBuilderTask>> CreateTasks(string instruction)
    {
        var promptAnalayzeInstruction = @"Create a list of tasks based on the specification by the user.";
        var generatorAnalayzeInstruction = new DocumentGenerator(Store, new List<ChatMessage>() { new SystemChatMessage(promptAnalayzeInstruction) }, OpenAiApiKey, LogFilePath);
        var response = await generatorAnalayzeInstruction.GetResponse<CodeBuilderTasks>(instruction, History, true, true);
        // todo store in tasks file
        var tasks = response.Tasks;

        return tasks;
    }
}
