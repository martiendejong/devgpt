// See https://aka.ms/new-console-template for more information
using DevGPT.NewAPI;
using MathNet.Numerics.Optimization;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

var openAISettings = OpenAISettings.Load();
string openAiApiKey = openAISettings.ApiKey;


//string appDir = @"C:\Projects\myhtmlgame";
//string documentStoreRoot = @"C:\Projects\myhtmlgame";
//string embeddingsFile = @"C:\Projects\myhtmlgame\embeddings";
//string logFilePath = @"C:\Projects\myhtmlgame\log";

// Vera frontend
string appDir = @"C:\Projects\socialmediahulp";
string documentStoreRoot = @"C:\Projects\socialmediahulp";
string embeddingsFile = @"C:\Projects\socialmediahulp\embeddings";
string logFilePath = @"C:\Projects\socialmediahulp\log";

string tempStoreRoot = @"C:\Projects\socialmediahulp\tempstore";
string tempEmbeddingsFile = @"C:\Projects\socialmediahulp\tempstore\embeddings";


var appFolderStoreConfig = new DocumentStoreConfig(documentStoreRoot, embeddingsFile, openAiApiKey);
var store = new DocumentStore(appFolderStoreConfig);

var builder = new CodeBuilder2(appDir, documentStoreRoot, embeddingsFile, openAiApiKey, logFilePath, tempStoreRoot, tempEmbeddingsFile);
builder.Output = Console.WriteLine;
//await builder.AddFiles(["*.js", "*.css", "*.html"]);
await builder.AddFiles(["*.js", "*.ts", "*.vue"], @"frontend\src");
await builder.AddFiles(["*.cs"], "", ["bin", "obj"]);
await builder.AddFiles(["*.cssproj"]);
await builder.AddFiles(["*.sln"]);
while (true)
{
    Console.WriteLine("Geef een instructie");
    var input = Console.ReadLine();
    await builder.Execute(input);
    builder.History.ForEach(m => Console.WriteLine(m.Content.ToString()));
}

return;





//var remove = store.Embeddings.Where(e => e.Path.Contains("LocationStore")).ToList();
//foreach(var item in remove)
//{
//    store.Embeddings.Remove(item);
//}
//var dir = new DirectoryInfo(@"C:\projects\beheerportaal\quasar\src");
//var files = dir.GetFiles("LocationStore.ts", SearchOption.AllDirectories);

//// todo only if store empty (first time)
//var dir = new DirectoryInfo(documentStoreRoot);
//var files = dir.GetFiles("*.js", SearchOption.AllDirectories)
//    .Concat(dir.GetFiles("*.ts", SearchOption.AllDirectories))
//    .Concat(dir.GetFiles("*.vue", SearchOption.AllDirectories))
//    .ToList();
//foreach (var file in files)
//{
//    var relPath = file.FullName.Substring((documentStoreRoot + "\\").Length);
//    await store.AddDocument(file.FullName, file.Name, relPath, false);
//}

//await store.UpdateEmbeddings();
//store.SaveEmbeddings();

var promptCreateTasks = @"You are a quasar developer working on the Vera AI App. Your goal is to write quasar code as clean as possible. You are an expert at SONAR, SOLID, design patterns, coding best practices and you apply them when possible. Maintain a task list in the file tasks.txt. Analyse the input of the user and with the new insights you gained update the task list.";
var promptPrepareTask = @"You are a quasar developer working on the Vera AI App. Your goal is to write quasar code as clean as possible. You are an expert at SONAR, SOLID, design patterns, coding best practices and you apply them when possible. Maintain a task list in the file tasks.txt. Analyse the tasks in the task list and the related code files. Split tasks into subtasks where neccessary. Add comments to tasks where this is appropriate.";
var promptExecuteTask = @"You are a quasar developer working on the Vera AI App. Your goal is to write quasar code as clean as possible. You are an expert at SONAR, SOLID, design patterns, coding best practices and you apply them when possible. Maintain a task list in the file tasks.txt. Pick the most urgent task in the tasklist and execute it. Label the task with TEST.";
var promptTestTask = @"You are a quasar developer working on the Vera AI App. Your goal is to write quasar code as clean as possible. You are an expert at SONAR, SOLID, design patterns, coding best practices and you apply them when possible. Maintain a task list in the file tasks.txt. Pick a task that has the label TEST. Verify that the task is complete. If not remove the label test and add rework comments.";
var promptTestBuild = @"You are a quasar developer working on the Vera AI App. Your goal is to write quasar code as clean as possible. You are an expert at SONAR, SOLID, design patterns, coding best practices and you apply them when possible. Maintain a task list in the file tasks.txt. Pick a task that has the label TEST. Run the build. See if there are any errors or warnings that are related to this task. If so remove the label TEST and add rework comments to solve the errors. If there are errors or warnings that are unrelated to this task add separate taks for them in the task list.";

var prompt = @"You are a quasar developer working on the Vera AI App. Your goal is to write quasar code as clean as possible. You are an expert at SONAR, SOLID, design patterns, coding best practices and you apply them when possible. Maintain a task list in the file tasks.txt. When a task is complete mark it as TEST.";

//.htaccess - protects the whole folder with a password
var promptMessages = new List<ChatMessage>() { new SystemChatMessage(prompt) };


var generator = new DocumentGenerator(store, promptMessages, appFolderStoreConfig.OpenAiApiKey, logFilePath, new List<IStore>());
//await generator.UpdateStore("Add a BrUserHub that works like the BuildingHub but for BrUsers. The BrUserHub should later be integrated in the BrUserStore as a separate task");


var generatorCreateTasks = new DocumentGenerator(store, new List<ChatMessage>() { new SystemChatMessage(promptCreateTasks) }, appFolderStoreConfig.OpenAiApiKey, logFilePath, new List<IStore>());
var generatorPrepareTask = new DocumentGenerator(store, new List<ChatMessage>() { new SystemChatMessage(promptPrepareTask) }, appFolderStoreConfig.OpenAiApiKey, logFilePath, new List<IStore>());
var generatorExecuteTask = new DocumentGenerator(store, new List<ChatMessage>() { new SystemChatMessage(promptExecuteTask) }, appFolderStoreConfig.OpenAiApiKey, logFilePath, new List<IStore>());
var generatorTestTask = new DocumentGenerator(store, new List<ChatMessage>() { new SystemChatMessage(promptTestTask) }, appFolderStoreConfig.OpenAiApiKey, logFilePath, new List<IStore>());
var generatorTestBuild = new DocumentGenerator(store, new List<ChatMessage>() { new SystemChatMessage(promptTestBuild) }, appFolderStoreConfig.OpenAiApiKey, logFilePath, new List<IStore>());



static Tuple<string, string> GetQuasarBuildOutput(string workingDirectory, string file, string logFile, string errorsFile)
{
    ProcessStartInfo psi = new ProcessStartInfo
    {
        WorkingDirectory = workingDirectory,
        FileName = workingDirectory + "\\" + file,
        //Arguments = "build",
        //RedirectStandardOutput = true,
        //RedirectStandardError = true,
        UseShellExecute = true
    };

    using (Process process = new Process { StartInfo = psi })
    {
        process.Start();

        // Read output and errors
        //string output = process.StandardOutput.ReadToEnd();
        //string error = process.StandardError.ReadToEnd();
        //var output = "";
        //var error = "";

        process.WaitForExit();

        var output = File.ReadAllText(workingDirectory + "\\" + logFile);
        var error = File.ReadAllText(workingDirectory + "\\" + errorsFile);

        return new Tuple<string, string>(output, error);
    }
}

var messages = new List<ChatMessage>();

var tools = new ToolsContextBase();
//tools.Add("Build", "Builds the Quasar app and returns the build output.", new List<ChatToolParameter>(), async (messages, call) =>
//{
//    var output = GetQuasarBuildOutput(appDir, buildFile, buildOutputFile, buildErrorsFile);
//    return output.Item2;
//});
//var tools = new StoreToolsContext(generator.SimpleApi.Model, openAiApiKey, store);

while (true)
{
    Console.WriteLine("Geef een instructie");
    var input = Console.ReadLine();
    await store.UpdateEmbeddings();
    store.SaveEmbeddings();
    messages.Add(new UserChatMessage(input));
    var response = await generatorCreateTasks.UpdateStore(messages, null, true, true, tools);
    messages.Add(new AssistantChatMessage(response));
    response = await generatorPrepareTask.UpdateStore(messages, null, true, true, tools);
    messages.Add(new AssistantChatMessage(response));
    response = await generatorExecuteTask.UpdateStore(messages, null, true, true, tools);
    messages.Add(new AssistantChatMessage(response));
    response = await generatorTestTask.UpdateStore(messages, null, true, true, tools);
    messages.Add(new AssistantChatMessage(response));
    response = await generatorTestBuild.UpdateStore(messages, null, true, true, tools);
    messages.Add(new AssistantChatMessage(response));
    Console.WriteLine(response);
}
