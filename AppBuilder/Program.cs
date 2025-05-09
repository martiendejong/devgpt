// See https://aka.ms/new-console-template for more information
using DevGPT.NewAPI;
using MathNet.Numerics.Optimization;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using Store.OpnieuwOpnieuw;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

var openAISettings = OpenAIConfig.Load();
string openAiApiKey = openAISettings.ApiKey;


//string appDir = @"C:\Projects\myhtmlgame";
//string documentStoreRoot = @"C:\Projects\myhtmlgame";
//string embeddingsFile = @"C:\Projects\myhtmlgame\embeddings";
//string logFilePath = @"C:\Projects\myhtmlgame\log";

// Vera frontend
//string appDir = @"C:\Projects\socialmediahulp";
//string documentStoreRoot = @"C:\Projects\socialmediahulp";
//string embeddingsFile = @"C:\Projects\socialmediahulp\embeddings";
//string partsFile = @"C:\Projects\socialmediahulp\parts";
//string logFilePath = @"C:\Projects\socialmediahulp\log";

//string tempStoreRoot = @"C:\Projects\socialmediahulp\tempstore";
//string tempEmbeddingsFile = @"C:\Projects\socialmediahulp\tempstore\embeddings";

//string tempPartsFile = @"C:\Projects\socialmediahulp\tempstore\parts";




string appDir = @"C:\Projects\devgpt";
string documentStoreRoot = @"C:\Projects\devgpt";
string embeddingsFile = @"C:\Projects\devgpt\embeddings";
string partsFile = @"C:\Projects\devgpt\parts";
string logFilePath = @"C:\Projects\devgpt\log";

string tempStoreRoot = @"C:\Projects\devgpt\tempstore";
string tempEmbeddingsFile = @"C:\Projects\devgpt\tempstore\embeddings";

string tempPartsFile = @"C:\Projects\devgpt\tempstore\parts";







//var appFolderStoreConfig = new DocumentStoreConfig(documentStoreRoot, embeddingsFile, openAiApiKey);
//var store = new DocumentStore(appFolderStoreConfig);

var builder = new CodeBuilder2(appDir, documentStoreRoot, embeddingsFile, partsFile, openAiApiKey, logFilePath, tempStoreRoot, tempEmbeddingsFile, tempPartsFile);
builder.Output = Console.WriteLine;
//await builder.AddFiles(["*.js", "*.css", "*.html"]);
//await builder.AddFiles(["*.js", "*.ts", "*.vue"], @"frontend\src");
//await builder.AddFiles(["*.cs"], "", ["bin", "obj"]);
//await builder.AddFiles(["*.cssproj"]);
//await builder.AddFiles(["*.sln"]);

await builder.AddFiles(["*.cs"], "", ["bin", "obj"]);
await builder.AddFiles(["*.cssproj"]);
await builder.AddFiles(["*.sln"]);


while (true)
{
    Console.WriteLine("Geef een instructie");
    var input = Console.ReadLine();
    await builder.Execute(input);
    builder.History.ForEach(m => Console.WriteLine(m.Text));
}

return;


