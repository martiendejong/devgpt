// See https://aka.ms/new-console-template for more information
using DevGPT.NewAPI;
using MathNet.Numerics.Optimization;
using OpenAI.Chat;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

string openAiApiKey = "***REMOVED***";

// Vera frontend
string appDir = @"C:\Projects\socialmediahulp\frontend";
string buildFile = "runquasar.bat";
string documentStoreRoot = @"C:\Projects\socialmediahulp\frontend\src";
string embeddingsFile = @"C:\Projects\socialmediahulp\frontend\embeddings";
string logFilePath = @"C:\Projects\socialmediahulp\frontend\log";

var appFolderStoreConfig = new DocumentStoreConfig(documentStoreRoot, embeddingsFile, openAiApiKey);
var store = new DocumentStore(appFolderStoreConfig);

//var remove = store.Embeddings.Where(e => e.Path.Contains("LocationStore")).ToList();
//foreach(var item in remove)
//{
//    store.Embeddings.Remove(item);
//}
//var dir = new DirectoryInfo(@"C:\projects\beheerportaal\quasar\src");
//var files = dir.GetFiles("LocationStore.ts", SearchOption.AllDirectories);

// todo only if store empty (first time)
var dir = new DirectoryInfo(documentStoreRoot);
var files = dir.GetFiles("*.js", SearchOption.AllDirectories)
    .Concat(dir.GetFiles("*.ts", SearchOption.AllDirectories))
    .Concat(dir.GetFiles("*.vue", SearchOption.AllDirectories))
    .ToList();
foreach (var file in files)
{
    var relPath = file.FullName.Substring((documentStoreRoot + "\\").Length);
    await store.AddDocument(file.FullName, file.Name, relPath, false);
}

await store.UpdateEmbeddings();
store.SaveEmbeddings();
var prompt =
@"You are a quasar developer working on the Vera AI App. Your goal is to write quasar code as clean as possible. You are an expert at SONAR, SOLID, design patterns, coding best practices and you apply them when possible. Maintain a task list in the file tasks.txt";

//.htaccess - protects the whole folder with a password
var promptMessages = new List<ChatMessage>() { new SystemChatMessage(prompt) };


var generator = new DocumentGenerator(store, promptMessages, appFolderStoreConfig.OpenAiApiKey, logFilePath);
//await generator.UpdateStore("Add a BrUserHub that works like the BuildingHub but for BrUsers. The BrUserHub should later be integrated in the BrUserStore as a separate task");



static Tuple<string, string> GetQuasarBuildOutput(string workingDirectory, string file)
{
    ProcessStartInfo psi = new ProcessStartInfo
    {
        WorkingDirectory = workingDirectory,
        FileName = workingDirectory + "\\" + file,
        //Arguments = "build",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };

    using (Process process = new Process { StartInfo = psi })
    {
        process.Start();

        // Read output and errors
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        //var output = "";
        //var error = "";

        process.WaitForExit();

        Console.WriteLine("Build Output:\n" + output);
        Console.WriteLine("Build Errors:\n" + error);
        var index = output.IndexOf("src/components/");
        if (index == -1)
            output = "";
        else
            output = output.Substring(index);

        return new Tuple<string, string>(output, error);
    }
}

var messages = new List<ChatMessage>();

while (true)
{
    Console.WriteLine("Geef een instructie");
    var input = Console.ReadLine();
    await store.UpdateEmbeddings();
    store.SaveEmbeddings();
    messages.Add(new UserChatMessage(input));
    var response = await generator.UpdateStore(messages);
    messages.Add(new AssistantChatMessage(response));
    Console.WriteLine(response);
}

