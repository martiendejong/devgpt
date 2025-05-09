// See https://aka.ms/new-console-template for more information
using MathNet.Numerics.Optimization;
using OpenAI.Chat;
using Store.OpnieuwOpnieuw;
using Store.OpnieuwOpnieuw.AIClient;
using Store.OpnieuwOpnieuw.DocumentStore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Runtime.Serialization;

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
var paths = new StorePaths(appDir);

string logFilePath = @"C:\Projects\devgpt\log";



var tmpPaths = new StorePaths(@"C:\Projects\devgpt\tempstore");



var openAIConfig = new OpenAIConfig(openAiApiKey);
var llmClient = new OpenAIClientWrapper(openAIConfig);


var builder = new CodeBuilder2(appDir, paths.RootFolder, paths.EmbeddingsFile, paths.PartsFile, openAiApiKey, logFilePath, tmpPaths.RootFolder, tmpPaths.EmbeddingsFile, tmpPaths.PartsFile);
builder.Output = Console.WriteLine;


await builder.AddFiles(["*.cs"], "", ["bin", "obj"]);
await builder.AddFiles(["*.cssproj"]);
await builder.AddFiles(["*.sln"]);




var store = CreateStore(paths, llmClient, "DevGPT_codebase");
var teamstore = CreateStore(tmpPaths, llmClient, "AITEAM_temp");

var factory = new AgentFactory(openAiApiKey, logFilePath);


var pmPrompt = @"Jij bent een projectmanager. Jij ontvangt de gebruikersprompt, verdeelt deze in logische deeltaken, en roept de LeadArchitect agent aan om deze taken verder uit te werken en uit te voeren.";
var pmPaths = new StorePaths(@"C:\Projects\devgpt\roles\projectmanager");
var pmStore = CreateStore(pmPaths, llmClient, "AITEAM_projectmanager");
IEnumerable<(DocumentStore Store, bool Write)> pmStores = [(store, true), (teamstore, true), (pmStore, true)];
List<string> pmFunctions = ["delegate"];
List<string> pmAgents = ["LeadArchitect"];
var projectManager = await factory.CreateAgent("ProjectManager", pmPrompt, pmStores, pmFunctions, pmAgents);

var architectPrompt = @"Jij bent een ervaren softwarearchitect. Jij analyseert gebruikersvragen, begrijpt de structuur en samenhang van de codebase, en plant oplossingsstappen. Je splitst taken in logische eenheden en roept gespecialiseerde agents aan om ze uit te voeren.";
var architectPaths = new StorePaths(@"C:\Projects\devgpt\roles\architect");
var architectstore = CreateStore(architectPaths, llmClient, "AITEAM_architect");
IEnumerable<(DocumentStore Store, bool Write)> architectStores = [(store, true), (teamstore, true), (architectstore, true)];
List<string> architectFunctions = ["git", "build", "delegate"];
List<string> architectAgents = ["CodeAnalyst", "CodeWriter", "CodeReviewer", "TestEngineer", "RefactorBot", "DocWriter"];
var leadArchitect = await factory.CreateAgent("LeadArchitect", architectPrompt, architectStores, architectFunctions, architectAgents);

var analystPrompt = @"Jij bent een code-analyse-expert. Je leest bestaande code en legt uit wat deze doet, inclusief afhankelijkheden en risico’s.";
var analystPaths = new StorePaths(@"C:\Projects\devgpt\roles\codeanalyst");
var analystStore = CreateStore(analystPaths, llmClient, "AITEAM_codeanalyst");
IEnumerable<(DocumentStore Store, bool Write)> analystStores = [(store, false), (teamstore, true), (analystStore, true)];
List<string> analystFunctions = ["read"];
List<string> analystAgents = [];
var codeAnalyst = await factory.CreateAgent("CodeAnalyst", analystPrompt, analystStores, analystFunctions, analystAgents);

var writerPrompt = @"Jij bent een professionele softwareontwikkelaar. Je schrijft nette, geteste en functionele code op basis van aangeleverde specificaties.";
var writerPaths = new StorePaths(@"C:\Projects\devgpt\roles\codewriter");
var writerStore = CreateStore(writerPaths, llmClient, "AITEAM_codewriter");
IEnumerable<(DocumentStore Store, bool Write)> writerStores = [(store, true), (teamstore, true), (writerStore, true)];
List<string> writerFunctions = ["read", "write"];
List<string> writerAgents = [];
var codeWriter = await factory.CreateAgent("CodeWriter", writerPrompt, writerStores, writerFunctions, writerAgents);

var reviewerPrompt = @"Jij bent een zeer kritische code reviewer. Je controleert code op leesbaarheid, consistentie, veiligheid en performance.";
var reviewerPaths = new StorePaths(@"C:\Projects\devgpt\roles\codereviewer");
var reviewerStore = CreateStore(reviewerPaths, llmClient, "AITEAM_codereviewer");
IEnumerable<(DocumentStore Store, bool Write)> reviewerStores = [(store, false), (teamstore, true), (reviewerStore, true)];
List<string> reviewerFunctions = ["read"];
List<string> reviewerAgents = [];
var codeReviewer = await factory.CreateAgent("CodeReviewer", reviewerPrompt, reviewerStores, reviewerFunctions, reviewerAgents);

var testerPrompt = @"Jij bent een testexpert. Jij ontwikkelt tests, voert builds uit, analyseert fouten en rapporteert betrouwbaar.";
var testerPaths = new StorePaths(@"C:\Projects\devgpt\roles\testengineer");
var testerStore = CreateStore(testerPaths, llmClient, "AITEAM_testengineer");
IEnumerable<(DocumentStore Store, bool Write)> testerStores = [(store, true), (teamstore, true), (testerStore, true)];
List<string> testerFunctions = ["read", "write", "build"];
List<string> testerAgents = [];
var testEngineer = await factory.CreateAgent("TestEngineer", testerPrompt, testerStores, testerFunctions, testerAgents);

var refactorPrompt = @"Jij bent gespecialiseerd in code-refactoren. Je herstructureert code voor betere leesbaarheid, onderhoudbaarheid of performance, zonder gedrag te wijzigen.";
var refactorPaths = new StorePaths(@"C:\Projects\devgpt\roles\refactorbot");
var refactorStore = CreateStore(refactorPaths, llmClient, "AITEAM_refactorbot");
IEnumerable<(DocumentStore Store, bool Write)> refactorStores = [(store, true), (teamstore, true), (refactorStore, true)];
List<string> refactorFunctions = ["read", "write"];
List<string> refactorAgents = [];
var refactorBot = await factory.CreateAgent("RefactorBot", refactorPrompt, refactorStores, refactorFunctions, refactorAgents);

var docPrompt = @"Jij schrijft bondige, accurate en bruikbare technische documentatie op basis van de codebase.";
var docPaths = new StorePaths(@"C:\Projects\devgpt\roles\docwriter");
var docStore = CreateStore(docPaths, llmClient, "AITEAM_docwriter");
IEnumerable<(DocumentStore Store, bool Write)> docStores = [(store, true), (teamstore, true), (docStore, true)];
List<string> docFunctions = ["read", "write"];
List<string> docAgents = [];
var docWriter = await factory.CreateAgent("DocWriter", docPrompt, docStores, docFunctions, docAgents);

factory.Messages = builder.History;






////var appFolderStoreConfig = new DocumentStoreConfig(documentStoreRoot, embeddingsFile, openAiApiKey);
////var store = new DocumentStore(appFolderStoreConfig);

//var builder = new CodeBuilder2(appDir, documentStoreRoot, embeddingsFile, partsFile, openAiApiKey, logFilePath, tempStoreRoot, tempEmbeddingsFile, tempPartsFile);
//builder.Output = Console.WriteLine;
////await builder.AddFiles(["*.js", "*.css", "*.html"]);
////await builder.AddFiles(["*.js", "*.ts", "*.vue"], @"frontend\src");
////await builder.AddFiles(["*.cs"], "", ["bin", "obj"]);
////await builder.AddFiles(["*.cssproj"]);
////await builder.AddFiles(["*.sln"]);


//await builder.AddFiles(["*.cs"], "", ["bin", "obj"]);
//await builder.AddFiles(["*.cssproj"]);
//await builder.AddFiles(["*.sln"]);


////await builder.AddFiles(["*.*"], "", ["*."]);


while (true)
{
    Console.WriteLine("Geef een instructie");
    var input = Console.ReadLine();
    var response = await projectManager.Generator.UpdateStore(input, builder.History, true, true, projectManager.Tools, null);
    builder.History.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = response });

    //await builder.Execute(input);
    //builder.History.ForEach(m => Console.WriteLine(m.Text));
}

return;

static DocumentStore CreateStore(StorePaths paths, OpenAIClientWrapper llmClient, string name)
{
    var embeddingStore = new EmbeddingFileStore(paths.EmbeddingsFile, llmClient);
    var textStore = new TextFileStore(paths.RootFolder);
    var partStore = new DocumentPartFileStore(paths.PartsFile);
    var store = new DocumentStore(embeddingStore, textStore, partStore, llmClient);
    store.Name = name;
    return store;
}

static DocumentStore CreateMemoryStore(string documentStoreRoot, string embeddingsFile, string partsFile, OpenAIClientWrapper llmClient, string n)
{
    var embeddingStore = new TextEmbeddingMemoryStore(llmClient);
    var textStore = new TextFileStore(documentStoreRoot);
    var partStore = new DocumentPartFileStore(partsFile);
    var store = new DocumentStore(embeddingStore, textStore, partStore, llmClient);
    store.Name = n;
    return store;
}