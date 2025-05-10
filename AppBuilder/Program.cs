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

#region Configuration and Constants

const string AppDirectory = @"C:\Projects\devgpt";
const string TempDirectory = @"C:\Projects\devgpt\tempstore";
const string LogFilePath = @"C:\Projects\devgpt\log";

const string ProjectManagerPrompt = "Jij bent een projectmanager. Jij ontvangt de gebruikersprompt, verdeelt deze in logische deeltaken, en roept de LeadArchitect agent aan om deze taken uit te voeren.";
const string ArchitectPrompt = "Jij bent een ervaren softwarearchitect. Jij begrijpt de structuur en samenhang van de codebase, en plant oplossingsstappen. Je splitst taken in logische eenheden en roept gespecialiseerde agents aan om ze uit te voeren.";
const string AnalystPrompt = "Jij bent een code-analyse-expert. Je leest bestaande code en legt uit wat deze doet, inclusief afhankelijkheden en risico’s.";
const string WriterPrompt = "Jij bent een professionele softwareontwikkelaar. Je schrijft nette, geteste en functionele code op basis van aangeleverde specificaties.";
const string ReviewerPrompt = "Jij bent een zeer kritische code reviewer. Je controleert code op leesbaarheid, consistentie, veiligheid en performance.";
const string TesterPrompt = "Jij bent een testexpert. Jij ontwikkelt tests, voert builds uit, analyseert fouten en rapporteert betrouwbaar.";
const string RefactorPrompt = "Jij bent gespecialiseerd in code-refactoren. Je herstructureert code voor betere leesbaarheid, onderhoudbaarheid of performance, zonder gedrag te wijzigen.";
const string DocPrompt = "Jij schrijft bondige, accurate en bruikbare technische documentatie op basis van de codebase.";

#endregion

#region Main Logic

var openAISettings = OpenAIConfig.Load();
string openAIApiKey = openAISettings.ApiKey;

var mainPaths = new StorePaths(AppDirectory);
var tempPaths = new StorePaths(TempDirectory);
var openAIConfig = new OpenAIConfig(openAIApiKey);
var llmClient = new OpenAIClientWrapper(openAIConfig);

var codeBuilder = new CodeBuilder2(
    AppDirectory, 
    mainPaths.RootFolder, 
    mainPaths.EmbeddingsFile, 
    mainPaths.PartsFile,
    openAIApiKey, 
    LogFilePath, 
    tempPaths.RootFolder, 
    tempPaths.EmbeddingsFile, 
    tempPaths.PartsFile);
codeBuilder.Output = Console.WriteLine;

await codeBuilder.AddFiles(["*.cs"], "", ["bin", "obj"]);
await codeBuilder.AddFiles(["*.cssproj"]);
await codeBuilder.AddFiles(["*.sln"]);

var codebaseStore = CreateStore(mainPaths, llmClient, "Codebase");
var teamStore = CreateStore(tempPaths, llmClient, "Teamdocumenten");

var agentFactory = new AgentFactory(openAIApiKey, LogFilePath);
agentFactory.Messages = codeBuilder.History;

// Agents
var projectManager = await CreateAgent(
    agentFactory,
    "ProjectManager",
    ProjectManagerPrompt,
    [ (codebaseStore, false), (teamStore, true), (CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\projectmanager"), llmClient, "Projectmanagerdocumenten"), true) ],
    ["delegate"],
    ["LeadArchitect"]);

var leadArchitect = await CreateAgent(
    agentFactory,
    "LeadArchitect",
    ArchitectPrompt,
    [ (codebaseStore, true), (teamStore, true), (CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\architect"), llmClient, "Architectdocumenten"), true) ],
    ["git", "build", "delegate"],
    ["CodeAnalyst", "CodeWriter", "CodeReviewer", "TestEngineer", "RefactorBot", "DocWriter"]);

var codeAnalyst = await CreateAgent(
    agentFactory,
    "CodeAnalyst",
    AnalystPrompt,
    [ (codebaseStore, false), (teamStore, true), (CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\codeanalyst"), llmClient, "Codeanalystdocumenten"), true) ],
    ["read"],
    []);

var codeWriter = await CreateAgent(
    agentFactory,
    "CodeWriter",
    WriterPrompt,
    [ (codebaseStore, true), (teamStore, true), (CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\codewriter"), llmClient, "Codewriterdocumenten"), true) ],
    ["read", "write"],
    []);

var codeReviewer = await CreateAgent(
    agentFactory,
    "CodeReviewer",
    ReviewerPrompt,
    [ (codebaseStore, false), (teamStore, true), (CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\codereviewer"), llmClient, "Codereviewerdocumenten"), true) ],
    ["read"],
    []);

var testEngineer = await CreateAgent(
    agentFactory,
    "TestEngineer",
    TesterPrompt,
    [ (codebaseStore, true), (teamStore, true), (CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\testengineer"), llmClient, "Testengineerdocumenten"), true) ],
    ["read", "write", "build"],
    []);

var refactorBot = await CreateAgent(
    agentFactory,
    "RefactorBot",
    RefactorPrompt,
    [ (codebaseStore, true), (teamStore, true), (CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\refactorbot"), llmClient, "Refactorbotdocumenten"), true) ],
    ["read", "write"],
    []);

var docWriter = await CreateAgent(
    agentFactory,
    "DocWriter",
    DocPrompt,
    [ (codebaseStore, true), (teamStore, true), (CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\docwriter"), llmClient, "Docwriterdocumenten"), true) ],
    ["read", "write"],
    []);

await HandleUserInput(projectManager, codeBuilder);

#endregion

#region Methods

/// <summary>
/// Centralized agent creation method.
/// </summary>
static async Task<DevGPTAgent> CreateAgent(
    AgentFactory factory,
    string name,
    string systemPrompt,
    IEnumerable<(DocumentStore Store, bool Write)> stores,
    IEnumerable<string> functions,
    IEnumerable<string> agents)
{
    return await factory.CreateAgent(name, systemPrompt, stores, functions, agents);
}

/// <summary>
/// Handles user input and project manager response loop.
/// </summary>
static async Task HandleUserInput(DevGPTAgent projectManager, CodeBuilder2 codeBuilder)
{
    while (true)
    {
        Console.WriteLine("Geef een instructie");
        var input = Console.ReadLine();
        var response = await projectManager.Generator.UpdateStore(input, codeBuilder.History, true, true, projectManager.Tools, null);
        codeBuilder.History.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = response });
    }
}

/// <summary>
/// Creates a document store for code and agent memory.
/// </summary>
static DocumentStore CreateStore(StorePaths paths, OpenAIClientWrapper llmClient, string name)
{
    var embeddingStore = new EmbeddingFileStore(paths.EmbeddingsFile, llmClient);
    var textStore = new TextFileStore(paths.RootFolder);
    var partStore = new DocumentPartFileStore(paths.PartsFile);
    var store = new DocumentStore(embeddingStore, textStore, partStore, llmClient);
    store.Name = name;
    return store;
}

#endregion
