using OpenAI.Chat;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System;
using System.Collections.Generic;
using System.Linq;

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
    public ToolsContextBase CoderTools {
        get
        {
            string[] coderToolNames = ["getfileslist", "getfile", "gettaskslist", "updatetask", "createtask", "git"];
            var coderTools = new ToolsContextBase();
            ChatTools.Where(t => coderToolNames.Contains(t.FunctionName)).ToList().ForEach(coderTools.Add);
            return coderTools;
        }
    }
    public ToolsContextBase ReviewerTools {
        get
        {
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

    public static string CoderPrompt = File.ReadAllText("CodePrompt.txt");
    public static string ReviewerPrompt = File.ReadAllText("ReviewPrompt.txt");

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
        Store = new DocumentStore(embeddingStore, textStore, partStore, llmClient);

        TempStoreFolderPath = tempStorePath;
        TempStoreEmbeddingsFilePath = tempStoreEmbeddingsFilePath;

        if (File.Exists(TempStoreEmbeddingsFilePath))
            File.Delete(TempStoreEmbeddingsFilePath);

        if (Directory.Exists(TempStoreFolderPath))
            File.Delete(TempStoreEmbeddingsFilePath);

        var tempEmbeddingStore = new EmbeddingFileStore(TempStoreEmbeddingsFilePath, llmClient);
        var tempTextStore = new TextFileStore(TempStoreFolderPath);
        var tempPartStore = new DocumentPartFileStore(tempStorePartsFilePath);
        TempStore = new DocumentStore(tempEmbeddingStore, tempTextStore, tempPartStore, llmClient);

        ChatTools =
        [
            new DevGPTChatTool("reflect", "Asks the reasoning assistant to reflect on a statement.", [ new() { Name = "statement", Description = "The statement that will be evaluated.", Required = true, Type = "string" } ], Reflect),
            new DevGPTChatTool("elaborate", "Asks the agent to generate an elaborate specification for an instruction. When you are asked to elaborate on something you are not allowed to call this function with the same instruction as that would cause a recursive loop.", [ new() { Name = "instruction", Description = "The instruction that needs elaboration", Required = true, Type = "string" } ], Elaborate),
            new DevGPTChatTool("getfile", "Retrieves a document from the store.", [ new() { Name = "path", Description = "The store relative path to the file.", Required = true, Type = "string" } ], GetFile),
            new DevGPTChatTool("getfileslist", "Gets the store files list.", [], GetFilesList),
            new DevGPTChatTool("gettaskslist", "Gets the current task list.", [], GetTasksList),
            new DevGPTChatTool("updatetask", "Updates the task with the given title.",[ new() { Name = "title", Description = "The title of the task to update", Required = true, Type = "string" }, new() { Name = "description", Description = "The updated description", Required = false, Type = "string" }, new() { Name = "status", Description = "The updated status. Allowed values: todo test or done", Required = false, Type = "string" } ], UpdateTask),
            new DevGPTChatTool("createtask", "Creates a new task.", [ new() { Name = "title", Description = "The title of the task", Required = true, Type = "string" }, new() { Name = "description", Description = "The description of the task", Required = true, Type = "string" } ], CreateTask),
            new DevGPTChatTool("git", "Calls git and returns the output.", [ new() { Name = "arguments", Description = "The arguments to call git with", Required = true, Type = "string" } ], Git),
            new DevGPTChatTool("build", "Builds the solution and returns the output.", [], Build)
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
    }

    // ---| STUBS FOR TOOLS ---
    private static Task<string> Reflect(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        throw new NotImplementedException("Reflect tool has not been implemented yet.");
    }
    private static Task<string> Elaborate(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        throw new NotImplementedException("Elaborate tool has not been implemented yet.");
    }
    private static Task<string> GetFile(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        throw new NotImplementedException("GetFile tool has not been implemented yet.");
    }
    private static Task<string> GetFilesList(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        throw new NotImplementedException("GetFilesList tool has not been implemented yet.");
    }
    private static Task<string> GetTasksList(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        throw new NotImplementedException("GetTasksList tool has not been implemented yet.");
    }
    private static Task<string> UpdateTask(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        throw new NotImplementedException("UpdateTask tool has not been implemented yet.");
    }
    private static Task<string> CreateTask(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        throw new NotImplementedException("CreateTask tool has not been implemented yet.");
    }
    private static Task<string> Git(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        throw new NotImplementedException("Git tool has not been implemented yet.");
    }
    private static Task<string> Build(List<DevGPTChatMessage> messages, DevGPTChatToolCall call)
    {
        throw new NotImplementedException("Build tool has not been implemented yet.");
    }
}
