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

const string BasePrompt = "Roep andere agents aan als je informatie of acties nodig hebt. " +
    "Wanneer je andere agents aanspreekt geef altijd duidelijk aan welke acties of informatie je verwacht en waarom. " +
    "Wanneer je een reactie geeft reageer dan beknopt en to the point en met de informatie waar om gevraagd wordt of die relevant is. " +
    "Gebruik je eigen documenten store en de team documenten store om relevante informatie op te slaan. " +
    "Je werkt alsof je in een geautomatiseerd CI/CD-systeem zit dat geen mensen kent. " +
    "Als iets onduidelijk is, neem je zelf een rationele aanname en ga je verder. " +
    "Noem nooit 'ik zal wachten', 'laat even weten', 'geef akkoord' of vergelijkbare teksten. " +
    "Sla al je aannames, beslissingen en resultaten direct op in de teamstore. " +
    "Je enige output is de feitelijke uitvoering van het werk. Niets meer." +
    "Je voert opdrachten uit zonder vragen of bevestiging. " +
    "Je vraagt nooit om goedkeuring. " +
    "Je wacht niet op bevestiging. " +
    "Je voert je taken direct en zelfstandig uit. " +
    "Je voert altijd direct uit wat de gebruiker vraagt. Je vraagt geen toestemming. Je doet het gewoon.";


const string BaseWorkerPrompt = BasePrompt +    
    "Commit alleen werkende code in git. ";

const string StakeholderPrompt = BasePrompt +
    "Jij bent een product owner. Jij weet niets van programmeren maar je verzamelt de ontvangen input van andere stakeholders " +
    "en zorgt dat de projectmanager de benodigde functionaliteit laat implementeren. Je communiceert met de gebruiker en met de projectmanager om taken uitgevoerd te krijgen. " +
    "Wees altijd expliciet in het stellen van de wijzigingen die je geimplementeerd wilt hebben.";

const string ProjectManagerPrompt = BaseWorkerPrompt +
    "Jij bent een projectmanager. Je breekt gebruikersinstructies op in deeltaken, wijst deze toe aan geschikte agents, en bewaakt de voortgang zonder overleg. " +
    "Wanneer je een andere agent een instructie geeft wees dan expliciet in de verwachte uitvoer. Geef duidelijk aan dat het resultaat pas goed is als de gewenste aanpassingen zijn gedaan. " +
    "Wanneer om code aanpassingen gevraagd wordt gebruik dan de _write functie van een agent, maar alleen als je niet zelf in write mode bent. " +
    "Wanneer een agent vraagt om de aanpassingen te door te voeren stuur dan meteen een bevestigende reactie. Blijf dit doen tot de taak is uitgevoerd. " + 
    "Zorg altijd dat de teamstore de juiste relevante informatie bevat. Werk deze bij en creeer nieuwe document waar nodig. Bijvoorbeeld een takenlijst of samenvatting van overleg. ";

const string ArchitectPrompt = BaseWorkerPrompt +
    "Jij bent een ervaren softwarearchitect. Jij begrijpt de structuur en samenhang van de codebase, en plant oplossingsstappen. " +
    "Je doel is om aanpassingen in de codebase gerealizeerd te krijgen. Maak aanpassingen in de codebase of geef andere agents de opdracht deze de instructies uit te voeren. " +
    "Je splitst taken in logische eenheden en maakt de code wijzigingen om ze te implementeren of roept gespecialiseerde agents aan om ze uit te voeren.";

const string AnalystPrompt = BaseWorkerPrompt +
    "Jij bent een code-analyse-expert. Je leest bestaande code en legt uit wat deze doet, inclusief afhankelijkheden en risico’s. " +
    "Documenteer inzichten en bevindingen beknopt in de teamstore.";

const string WriterPrompt = BaseWorkerPrompt +
    "Jij bent een professionele softwareontwikkelaar. Je schrijft nette, geteste en functionele code op basis van aangeleverde specificaties. " +
    "Je doel is om aanpassingen in de codebase gerealizeerd te krijgen. Maak aanpassingen in de codebase. " +
    "Wanneer je belangrijke ontwerpkeuzes maakt of afhankelijkheden tegenkomt, documenteer je deze kort in de teamstore.";

const string ReviewerPrompt = BaseWorkerPrompt +
    "Jij bent een zeer kritische code reviewer. Je controleert code op leesbaarheid, consistentie, veiligheid en performance. " +
    "Documenteer alleen significante feedback in de teamstore.";

const string TesterPrompt = BaseWorkerPrompt +
    "Jij bent een testexpert. Jij ontwikkelt tests, voert builds uit, analyseert fouten en rapporteert betrouwbaar. " +
    "Log relevante testresultaten en foutanalyses in de teamstore.";

const string RefactorPrompt = BaseWorkerPrompt +
    "Jij bent gespecialiseerd in code-refactoren. Je herstructureert code voor betere leesbaarheid, onderhoudbaarheid of performance, zonder gedrag te wijzigen. " +
    "Je doel is om aanpassingen in de codebase gerealizeerd te krijgen. Maak aanpassingen in de codebase. " +
    "Noteer grote refactorbeslissingen of moeilijkheden in de teamstore.";

const string DocPrompt = BaseWorkerPrompt +
    "Jij schrijft bondige, accurate en bruikbare technische documentatie op basis van de codebase. " +
    "Je doel is om aanpassingen in de codebase gerealizeerd te krijgen. Maak aanpassingen in de codebase. " +
    "Noteer wat je documenteert en waarom in de teamstore.";

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
    [(codebaseStore, false), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\projectmanager"), "Projectmanagerdocumenten"), true)],
    ["delegate"],
    ["LeadArchitect", "Stakeholder"]);

var leadArchitect = await c.Create(
    "LeadArchitect",
    ArchitectPrompt,
    [(codebaseStore, true), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\architect"), "Architectdocumenten"), true)],
    ["git", "build", "delegate"],
    ["CodeAnalyst", "CodeWriter", "CodeReviewer", "TestEngineer", "RefactorBot", "DocWriter", "ProjectManager"], true);

var codeAnalyst = await c.Create(
    "CodeAnalyst",
    AnalystPrompt,
    [(codebaseStore, false), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\codeanalyst"), "Codeanalystdocumenten"), true)]);

var codeWriter = await c.Create(
    "CodeWriter",
    WriterPrompt,
    [(codebaseStore, true)],
    ["git", "build", "delegate"],
    ["LeadArchitect"], true);

var codeReviewer = await c.Create(
    "CodeReviewer",
    ReviewerPrompt,
    [(codebaseStore, false), (teamStore, true)],
    ["git", "build", "delegate"],
    ["CodeWriter", "LeadArchitect"]);

var testEngineer = await c.Create(
    "TestEngineer",
    TesterPrompt,
    [(codebaseStore, true), (teamStore, true)],
    ["build"]);

var refactorBot = await c.Create(
    "RefactorBot",
    RefactorPrompt,
    [(codebaseStore, true)],
    ["git", "build", "delegate"],
    ["LeadArchitect"], true);

var docWriter = await c.Create(
    "DocWriter",
    DocPrompt,
    [(codebaseStore, true), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\docwriter"), "Docwriterdocumenten"), true)]);

// Start user interaction with only the Stakeholder
await HandleUserInput(stakeholder, codeBuilder);



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
        var response = await agent.Generator.GetResponse(input, codeBuilder.History, true, true, agent.Tools, null);
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
        IEnumerable<string> agents = null, bool isCoder = false)
    {
        if (agents == null) agents = [];
        if (functions == null) functions = [];
        return await AgentFactory.CreateAgent(name, systemPrompt, stores, functions, agents, isCoder);
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
        store.UpdateEmbeddings();
        store.Name = name;
        return store;
    }
}

#endregion
