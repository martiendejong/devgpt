// See https://aka.ms/new-console-template for more information
using DevGPT.NewAPI;
using OpenAI.Chat;
using System.Runtime.ConstrainedExecution;
using System.Text.Json;
using System.Threading.Tasks;

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

        var success = false;
        while (!success) {

            History.Add(instruction);
            Output("Analyze input");

            var specification = await CreateSpecification(instruction);

            var totalTask = new CodeBuilderTask { Title = instruction, Description = specification };

            Output("Create tasks");

            var tasks = await CreateTasks(specification);

            Output("Execute tasks");

            await ExecuteTasks(tasks);

            success = await VerifyTask(totalTask);
        }

        await Finish(instruction);
    }

    private async Task ExecuteTasks(List<CodeBuilderTask> tasks)
    {
        foreach (var task in tasks)
        {
            await ExecuteTask(task);
            var success = await VerifyTask(task);
            while (!success)
            {
                await ExecuteTask(task);
                success = await VerifyTask(task);
            }
        }
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

        LogAddMessage(task.Title);

        var instruction = $"{task.Title}\n{task.Description}";

        var specification = await CreateSpecification(instruction);

        // Get current branch name
        var branchResult = GitOutput.GetGitOutput(DocumentStoreFolderPath, "rev-parse --abbrev-ref HEAD");
        string branch = branchResult.Item1.Trim();

        // Get current commit hash
        var commitResult = GitOutput.GetGitOutput(DocumentStoreFolderPath, "rev-parse HEAD");
        string commit = commitResult.Item1.Trim();

        var promptExecute = @$"You are an expert software engineer. You will use Git Flow to commit and test your changes. You are now in branch {branch} at commit {commit}. Execute the task as specified.";
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
        Output("Verifying");

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
