using MathNet.Numerics.Optimization;
using OpenAI.Chat;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json;





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






//var codebaseStore = c.CreateStore(mainPaths, "Codebase");
//var teamStore = c.CreateStore(tempPaths, "Teamdocumenten");

//const string BasePrompt = "Roep andere agents aan als je informatie of acties nodig hebt. " +
//    "Wanneer je andere agents aanspreekt geef altijd duidelijk aan welke acties of informatie je verwacht en waarom. " +
//    "Wanneer je een reactie geeft reageer dan beknopt en to the point en met de informatie waar om gevraagd wordt of die relevant is. " +
//    "Gebruik je eigen documenten store en de team documenten store om relevante informatie op te slaan. " +
//    "Je werkt alsof je in een geautomatiseerd CI/CD-systeem zit dat geen mensen kent. " +
//    "Als iets onduidelijk is, neem je zelf een rationele aanname en ga je verder. " +
//    "Noem nooit 'ik zal wachten', 'laat even weten', 'geef akkoord' of vergelijkbare teksten. " +
//    "Sla al je aannames, beslissingen en resultaten direct op in de teamstore. " +
//    "Je enige output is de feitelijke uitvoering van het werk. Niets meer." +
//    "Je voert opdrachten uit zonder vragen of bevestiging. " +
//    "Je vraagt nooit om goedkeuring. " +
//    "Je wacht niet op bevestiging. " +
//    "Je voert je taken direct en zelfstandig uit. " +
//    "Je voert altijd direct uit wat de gebruiker vraagt. Je vraagt geen toestemming. Je doet het gewoon.";

//const string BaseWorkerPrompt = BasePrompt +    
//    "Commit alleen werkende code in git. ";

//const string StakeholderPrompt = BasePrompt +
//    "Jij bent een product owner. Jij weet niets van programmeren maar je verzamelt de ontvangen input van andere stakeholders " +
//    "en zorgt dat de projectmanager de benodigde functionaliteit laat implementeren. Je communiceert met de gebruiker en met de projectmanager om taken uitgevoerd te krijgen. " +
//    "Wees altijd expliciet in het stellen van de wijzigingen die je geimplementeerd wilt hebben.";

//const string ProjectManagerPrompt = BaseWorkerPrompt +
//    "Jij bent een projectmanager. Je breekt gebruikersinstructies op in deeltaken, wijst deze toe aan geschikte agents, en bewaakt de voortgang zonder overleg. " +
//    "Wanneer je een andere agent een instructie geeft wees dan expliciet in de verwachte uitvoer. Geef duidelijk aan dat het resultaat pas goed is als de gewenste aanpassingen zijn gedaan. " +
//    "Wanneer om code aanpassingen gevraagd wordt gebruik dan de _write functie van een agent, maar alleen als je niet zelf in write mode bent. " +
//    "Wanneer een agent vraagt om de aanpassingen te door te voeren stuur dan meteen een bevestigende reactie. Blijf dit doen tot de taak is uitgevoerd. " + 
//    "Zorg altijd dat de teamstore de juiste relevante informatie bevat. Werk deze bij en creeer nieuwe document waar nodig. Bijvoorbeeld een takenlijst of samenvatting van overleg. ";

//const string ArchitectPrompt = BaseWorkerPrompt +
//    "Jij bent een ervaren softwarearchitect. Jij begrijpt de structuur en samenhang van de codebase, en plant oplossingsstappen. " +
//    "Je doel is om aanpassingen in de codebase gerealizeerd te krijgen. Maak aanpassingen in de codebase of geef andere agents de opdracht deze de instructies uit te voeren. " +
//    "Je splitst taken in logische eenheden en maakt de code wijzigingen om ze te implementeren of roept gespecialiseerde agents aan om ze uit te voeren.";

//const string AnalystPrompt = BaseWorkerPrompt +
//    "Jij bent een code-analyse-expert. Je leest bestaande code en legt uit wat deze doet, inclusief afhankelijkheden en risico’s. " +
//    "Documenteer inzichten en bevindingen beknopt in de teamstore.";

//const string WriterPrompt = BaseWorkerPrompt +
//    "Jij bent een professionele softwareontwikkelaar. Je schrijft nette, geteste en functionele code op basis van aangeleverde specificaties. " +
//    "Je doel is om aanpassingen in de codebase gerealizeerd te krijgen. Maak aanpassingen in de codebase. " +
//    "Wanneer je belangrijke ontwerpkeuzes maakt of afhankelijkheden tegenkomt, documenteer je deze kort in de teamstore.";

//const string ReviewerPrompt = BaseWorkerPrompt +
//    "Jij bent een zeer kritische code reviewer. Je controleert code op leesbaarheid, consistentie, veiligheid en performance. " +
//    "Documenteer alleen significante feedback in de teamstore.";

//const string TesterPrompt = BaseWorkerPrompt +
//    "Jij bent een testexpert. Jij ontwikkelt tests, voert builds uit, analyseert fouten en rapporteert betrouwbaar. " +
//    "Log relevante testresultaten en foutanalyses in de teamstore.";

//const string RefactorPrompt = BaseWorkerPrompt +
//    "Jij bent gespecialiseerd in code-refactoren. Je herstructureert code voor betere leesbaarheid, onderhoudbaarheid of performance, zonder gedrag te wijzigen. " +
//    "Je doel is om aanpassingen in de codebase gerealizeerd te krijgen. Maak aanpassingen in de codebase. " +
//    "Noteer grote refactorbeslissingen of moeilijkheden in de teamstore.";

//const string DocPrompt = BaseWorkerPrompt +
//    "Jij schrijft bondige, accurate en bruikbare technische documentatie op basis van de codebase. " +
//    "Je doel is om aanpassingen in de codebase gerealizeerd te krijgen. Maak aanpassingen in de codebase. " +
//    "Noteer wat je documenteert en waarom in de teamstore.";







//var agentConfigs = new List<AgentConfig>();
//agentConfigs.Add(new AgentConfig
//{
//    Name = "Stakeholder",
//    Description = "The stakeholder collect feature requests and feedback from the end users and adds them to the task management system. Then calls the projectmanager to have the team implement them.",
//    Prompt = "Convert the user input into tasks with the label FEATURE and store them in the task management system. Call the projectmanager to start working on the newly added tasks.",
//    Stores = new List<StoreRef> { new() { Name = "Taskmanagement", Write = true } },
//    Functions = new List<string> { },
//    CallsAgents = new List<string> { "Projectmanager" },
//});
//agentConfigs.Add(new AgentConfig
//{
//    Name = "Projectmanager",
//    Description = "The projectmanager manages the task management system and assigns tasks to team members.",
//    Prompt = "For each requested tasks with the label FEATURE create tasks with the label TODO to implement the feature. Assign each tasks to a developer. Then call the developers to execute the tasks. When a task has the label TEST call a reviewer to review the test.",
//    Stores = new List<StoreRef> { new() { Name = "Taskmanagement", Write = true } },
//    Functions = new List<string> { },
//    CallsAgents = new List<string> { "C# developer", "Code reviewer" },
//});
//agentConfigs.Add(new AgentConfig
//{
//    Name = "C# developer",
//    Description = "A developer who is specialized in C# and ASP.NET core.",
//    Prompt = "Modify the files in the codebase to implement the requested changes. Then update the relevant tasks to label TEST.",
//    Stores = new List<StoreRef> { new() { Name = "Codebase", Write = true }, new() { Name = "Taskmanagement", Write = true } },
//    Functions = new List<string> { "git", "build" },
//    CallsAgents = new List<string> { "Code reviewer", "Projectmanager" },
//    ExplicitModify = true
//});
//agentConfigs.Add(new AgentConfig
//{
//    Name = "Code reviewer",
//    Description = "A developer who is specialized in reviewing code.",
//    Prompt = "Review the requested changes to see if there are any errors. Use the build and compare the uncommitted git changes to verify that the code is good and has not created any problems. If there are any problems update the relevant tasks and set the label from TEST to TODO. If there are no problems set the task to DONE.",
//    Stores = new List<StoreRef> { new() { Name = "Codebase", Write = false }, new() { Name = "Taskmanagement", Write = true } },
//    Functions = new List<string> { "git", "build" },
//    CallsAgents = new List<string> { "C# developer", "Projectmanager" },
//});
//agentConfigs.Add(new AgentConfig
//{
//    Name = "Quasar developer",
//    Description = "A frontend developer who is specialized in Vue and Quasar.",
//    Prompt = "Modify the files in the codebase to implement the requested changes.",
//    Stores = new List<StoreRef> { new() { Name = "Codebase", Write = true }, new() { Name = "StakeholderDocuments", Write = true } },
//    Functions = new List<string> { "git", "build" },
//    CallsAgents = new List<string> { "Piet", "Klaas" },
//    IsCoder = true
//});


//var codebaseConfig = new StoreConfig
//{
//    Name = "Codebase",
//    Path = AppDirectory
//};
//var taskManagementConfig = new StoreConfig
//{
//    Name = "Taskmanagement",
//    Path = AppDirectory
//};
//var storeConfigs = new List<StoreConfig>();
//storeConfigs.Add(codebaseConfig);

var storesjson = File.ReadAllText("stores.json");
var storesConfig = JsonSerializer.Deserialize<List<StoreConfig>>(storesjson);
var agentsjson = File.ReadAllText("agents.json");
var agentConfigs = JsonSerializer.Deserialize<List<AgentConfig>>(agentsjson);

var stores = storesConfig.Select(sc => c.CreateStore(new StorePaths(sc.Path), sc.Name) as IDocumentStore).ToList();
var agents = new List<DevGPTAgent>();
foreach(var ac in agentConfigs)
{
    var agent = await c.Create(ac.Name, ac.Prompt, ac.Stores.Select(acs => (stores.First(s => s.Name == acs.Name), true)).ToList(), ac.Functions, ac.CallsAgents, ac.ExplicitModify);
    agents.Add(agent);
}

await HandleUserInput(agents.First(), codeBuilder);

//File.WriteAllText("stores.json", JsonSerializer.Serialize(storeConfigs));
//File.WriteAllText("agents.json", JsonSerializer.Serialize(agentConfigs));

return;






//var stores = new List<IDocumentStore>();
//var codebaseStore = c.CreateStore(new StorePaths(codebaseConfig.Path), codebaseConfig.Name);
//stores.Add(codebaseStore);

//var agents = new List<DevGPTAgent>();

//var agent = await c.Create(config.Name, config.Prompt, config.Stores.Select(sc => (stores.First(s => s.Name == sc.Name), sc.Write)), config.Functions, config.CallsAgents, config.IsCoder);
//agents.Add(agent);





////File.ReadAllText("agents.json");

//// Agents
//var stakeholder = await c.Create(
//    "Stakeholder",
//    StakeholderPrompt,
//    [(teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\stakeholder"), "Stakeholderdocumenten"), true)],
//    ["delegate"],
//    ["ProjectManager"]);

//var projectManager = await c.Create(
//    "ProjectManager",
//    ProjectManagerPrompt,
//    [(codebaseStore, false), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\projectmanager"), "Projectmanagerdocumenten"), true)],
//    ["delegate"],
//    ["LeadArchitect", "Stakeholder"]);

//var leadArchitect = await c.Create(
//    "LeadArchitect",
//    ArchitectPrompt,
//    [(codebaseStore, true), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\architect"), "Architectdocumenten"), true)],
//    ["git", "build", "delegate"],
//    ["CodeAnalyst", "CodeWriter", "CodeReviewer", "TestEngineer", "RefactorBot", "DocWriter", "ProjectManager"], true);

//var codeAnalyst = await c.Create(
//    "CodeAnalyst",
//    AnalystPrompt,
//    [(codebaseStore, false), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\codeanalyst"), "Codeanalystdocumenten"), true)]);

//var codeWriter = await c.Create(
//    "CodeWriter",
//    WriterPrompt,
//    [(codebaseStore, true)],
//    ["git", "build", "delegate"],
//    ["LeadArchitect"], true);

//var codeReviewer = await c.Create(
//    "CodeReviewer",
//    ReviewerPrompt,
//    [(codebaseStore, false), (teamStore, true)],
//    ["git", "build", "delegate"],
//    ["CodeWriter", "LeadArchitect"]);

//var testEngineer = await c.Create(
//    "TestEngineer",
//    TesterPrompt,
//    [(codebaseStore, true), (teamStore, true)],
//    ["build"]);

//var refactorBot = await c.Create(
//    "RefactorBot",
//    RefactorPrompt,
//    [(codebaseStore, true)],
//    ["git", "build", "delegate"],
//    ["LeadArchitect"], true);

//var docWriter = await c.Create(
//    "DocWriter",
//    DocPrompt,
//    [(codebaseStore, true), (teamStore, true), (c.CreateStore(new StorePaths(@"C:\Projects\devgpt\roles\docwriter"), "Docwriterdocumenten"), true)]);



// Start user interaction with only the Stakeholder
//await HandleUserInput(stakeholder, codeBuilder);





/// <summary>
/// Handles user input and project manager response loop.
/// </summary>
static async Task HandleUserInput(DevGPTAgent agent, CodeBuilder2 codeBuilder)
{
    while (true)
    {
        Console.WriteLine("Geef een instructie");
        var input = Console.ReadLine();
        var response = await agent.Generator.GetResponse<IsReadyResult>(input, codeBuilder.History, true, true, agent.Tools, null);
        while (!response.IsRequestImplemented)
        {
            response = await agent.Generator.GetResponse<IsReadyResult>("Continue implementing the requested features", codeBuilder.History, true, true, agent.Tools, null);
            Console.WriteLine(response);
        }
        codeBuilder.History.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = response.Message });
    }
}