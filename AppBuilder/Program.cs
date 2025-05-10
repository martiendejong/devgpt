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

//  Configuration and Constants
const string AppDirectory = @"C:\Projects\devgpt";
const string TempDirectory = @"C:\Projects\devgpt\tempstore";
const string LogFilePath = @"C:\Projects\devgpt\log";

// main setup/init
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

var agentFactory = new AgentFactory(openAIApiKey, LogFilePath);
agentFactory.Messages = codeBuilder.History;

var c = new QuickAgentCreator(agentFactory, llmClient);
var codebaseStore = c.CreateStore(mainPaths, "Codebase");
var teamStore = c.CreateStore(tempPaths, "Teamdocumenten");

await codebaseStore.UpdateEmbeddings();

// agents

#region Agent prompts

const string BasePrompt = "Wanneer je andere agents aanspreekt geef altijd duidelijk aan welke acties of informatie je verwacht en waarom. Wanneer je een reactie geeft reageer dan beknopt en to the point en met de informatie waar om gevraagd wordt of die relevant is.";
const string StakeholderPrompt = BasePrompt + "Jij bent een product owner. Jij weet niets van programmeren maar je verzameld de ontvangen input van andere stakeholders en zorgt dat de projectmanager de benodigde functionaliteit laat implementeren.";

const string BaseWorkerPrompt = BasePrompt + "Voer de gevraagde instructies meteen uit. Jij bent de expert die weet wat je moet doen. Stel alleen vragen wanneer strict noodzakelijk. Commit alleen werkende code in git. ";
const string ProjectManagerPrompt = BaseWorkerPrompt + "Jij bent een projectmanager. Jij ontvangt de gebruikersprompt, verdeelt deze in logische deeltaken, en roept de LeadArchitect agent aan om deze taken uit te voeren.";
const string ArchitectPrompt = BaseWorkerPrompt + "Jij bent een ervaren softwarearchitect. Jij begrijpt de structuur en samenhang van de codebase, en plant oplossingsstappen. Je splitst taken in logische eenheden en roept gespecialiseerde agents aan om ze uit te voeren.";
const string AnalystPrompt = BaseWorkerPrompt + "Jij bent een code-analyse-expert. Je leest bestaande code en legt uit wat deze doet, inclusief afhankelijkheden en risico’s.";
const string WriterPrompt = BaseWorkerPrompt + "Jij bent een professionele softwareontwikkelaar. Je schrijft nette, geteste en functionele code op basis van aangeleverde specificaties.";
const string ReviewerPrompt = BaseWorkerPrompt + "Jij bent een zeer kritische code reviewer. Je controleert code op leesbaarheid, consistentie, veiligheid en performance.";
const string TesterPrompt = BaseWorkerPrompt + "Jij bent een testexpert. Jij ontwikkelt tests, voert builds uit, analyseert fouten en rapporteert betrouwbaar.";
const string RefactorPrompt = BaseWorkerPrompt + "Jij bent gespecialiseerd in code-refactoren. Je herstructureert code voor betere leesbaarheid, onderhoudbaarheid of performance, zonder gedrag te wijzigen.";
const string DocPrompt = BaseWorkerPrompt + "Jij schrijft bondige, accurate en bruikbare technische documentatie op basis van de codebase.";

#endregion

#region Main Logic

// Agents
var stakeholder = await c.Create(
    "Stakeholder",
    StakeholderPrompt,
    [(teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\stakeholder"), "Stakeholderdocumenten"), true)],
    ["delegate"],
    ["ProjectManager"]);

var projectManager = await c.Create(
    "ProjectManager",
    ProjectManagerPrompt,
    [ (codebaseStore, false), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\projectmanager"), "Projectmanagerdocumenten"), true) ],
    ["delegate"],
    ["LeadArchitect"]);

var leadArchitect = await c.Create(
    "LeadArchitect",
    ArchitectPrompt,
    [ (codebaseStore, true), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\architect"), "Architectdocumenten"), true) ],
    ["git", "build", "delegate"],
    ["CodeAnalyst", "CodeWriter", "CodeReviewer", "TestEngineer", "RefactorBot", "DocWriter"]);

var codeAnalyst = await c.Create(
    "CodeAnalyst",
    AnalystPrompt,
    [ (codebaseStore, false), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\codeanalyst"), "Codeanalystdocumenten"), true) ]);

var codeWriter = await c.Create(
    "CodeWriter",
    WriterPrompt,
    [ (codebaseStore, true), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\codewriter"), "Codewriterdocumenten"), true) ]);

var codeReviewer = await c.Create(
    "CodeReviewer",
    ReviewerPrompt,
    [ (codebaseStore, false), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\codereviewer"), "Codereviewerdocumenten"), true) ]);

var testEngineer = await c.Create(
    "TestEngineer",
    TesterPrompt,
    [ (codebaseStore, true), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\testengineer"), "Testengineerdocumenten"), true) ],
    ["build"]);

var refactorBot = await c.Create(
    "RefactorBot",
    RefactorPrompt,
    [ (codebaseStore, true), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\refactorbot"), "Refactorbotdocumenten"), true) ]);

var docWriter = await c.Create(
    "DocWriter",
    DocPrompt,
    [ (codebaseStore, true), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\docwriter"), "Docwriterdocumenten"), true) ]);

await HandleUserInput(stakeholder, codeBuilder);

#endregion

#region Methods


/// <summary>
/// Handles user input and project manager response loop.
/// </summary>
static async Task HandleUserInput(DevGPTAgent agent, CodeBuilder2 codeBuilder)
{
    while (true)
    {
        Console.WriteLine("Geef een instructie");
        var input = Console.ReadLine();
        var response = await agent.Generator.UpdateStore(input, codeBuilder.History, true, true, agent.Tools, null);
        codeBuilder.History.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = response });
    }
}

public class QuickAgentCreator
{
    public QuickAgentCreator(AgentFactory f, ILLMClient client)
    {
        AgentFactory = f;
        Client = client;
    }

    public AgentFactory AgentFactory { get; set; }
    public ILLMClient Client{ get; set; }

    /// <summary>
    /// Centralized agent creation method.
    /// </summary>
    public async Task<DevGPTAgent> Create(
        string name,
        string systemPrompt,
        IEnumerable<(DocumentStore Store, bool Write)> stores,
        IEnumerable<string> functions = null,
        IEnumerable<string> agents = null)
    {
        if (agents == null) agents = [];
        if (functions == null) functions = [];
        return await AgentFactory.CreateAgent(name, systemPrompt, stores, functions, agents);
    }

    /// <summary>
    /// Creates a document store for code and agent memory.
    /// </summary>
    public DocumentStore CreateStore(StorePaths paths, string name)
    {
        var embeddingStore = new EmbeddingFileStore(paths.EmbeddingsFile, Client);
        var textStore = new TextFileStore(paths.RootFolder);
        var partStore = new DocumentPartFileStore(paths.PartsFile);
        var store = new DocumentStore(embeddingStore, textStore, partStore, Client);
        store.Name = name;
        return store;
    }
}

#endregion
