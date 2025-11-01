using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls; // Correct toegevoegd
using Microsoft.Win32;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
namespace DevGPT.App.Windows;
// Eventueel missing modelimports, aannemende dat volgende types lokaal zijn:
// Als deze elders staan (bijv. in DevGPT.AgentFactory) graag correcte using plaatsen.
// using DevGPT.AgentFactory; // Bijvoorbeeld als FlowCardModel, FlowCardsBindingModel, FlowConfig hier vandaan komen.

public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string storesDevGPTRaw = string.Empty;
        private string agentsDevGPTRaw = string.Empty;
        private string flowsDevGPTRaw = string.Empty;
        private string storesFilePath = null;
        private string agentsFilePath = null;
        private string flowsFilePath = null;
        private bool storesLoaded = false;
        private bool agentsLoaded = false;
        private bool flowsLoaded = false;
        private List<StoreConfig> parsedStores = new List<StoreConfig>();
        private List<AgentConfig> parsedAgents = new List<AgentConfig>();
        private List<FlowConfig> parsedFlows = new List<FlowConfig>();
        private bool suppressStoresEditorSync = false;
        private bool suppressAgentsEditorSync = false;
        private bool suppressFlowsEditorSync = false;
        private List<string> _agentsAndFlows = [];
        public List<string> AgentsAndFlows { get => _agentsAndFlows; set { _agentsAndFlows = value; OnPropertyChanged(nameof(AgentsAndFlows)); } }
        private string _selectedAgentOrFlow = "";
        public string SelectedAgentOrFlow { get => _selectedAgentOrFlow; set { _selectedAgentOrFlow = value; OnPropertyChanged(nameof(_selectedAgentOrFlow)); } }
        private bool _isChatVisible = false;
        public bool IsChatVisible { get => _isChatVisible; set { _isChatVisible = value; OnPropertyChanged(nameof(IsChatVisible)); } }
        private bool _isOpeningChat = false;
        public bool IsOpeningChat { get => _isOpeningChat; set { _isOpeningChat = value; OnPropertyChanged(nameof(IsOpeningChat)); } }
        private bool _isOpenChatButtonEnabled = true;
        public bool IsOpenChatButtonEnabled { get => _isOpenChatButtonEnabled; set { _isOpenChatButtonEnabled = value; OnPropertyChanged(nameof(IsOpenChatButtonEnabled)); } }
        private UserAppConfig appConfig;
        private string configFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appconfig.json");
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            StoresDevGPTEditor.TextChanged += StoresDevGPTEditor_TextChanged;
            AgentsDevGPTEditor.TextChanged += AgentsDevGPTEditor_TextChanged;
            FlowsDevGPTEditor.TextChanged += FlowsDevGPTEditor_TextChanged;
            LoadLastUsedConfigPaths();
        }

        private void LoadLastUsedConfigPaths()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    appConfig = JsonSerializer.Deserialize<UserAppConfig>(File.ReadAllText(configFilePath));
                }
                else
                {
                    appConfig = new UserAppConfig();
                }
            }
            catch
            {
                appConfig = new UserAppConfig();
            }
            if (!string.IsNullOrWhiteSpace(appConfig.StoresFile) && File.Exists(appConfig.StoresFile))
            {
                storesFilePath = appConfig.StoresFile;
                TryLoadStoresFromFile(storesFilePath);
            }
            if (!string.IsNullOrWhiteSpace(appConfig.AgentsFile) && File.Exists(appConfig.AgentsFile))
            {
                agentsFilePath = appConfig.AgentsFile;
                TryLoadAgentsFromFile(agentsFilePath);
            }
            if (!string.IsNullOrWhiteSpace(appConfig.FlowsFile) && File.Exists(appConfig.FlowsFile))
            {
                flowsFilePath = appConfig.FlowsFile;
                TryLoadFlowsFromFile(flowsFilePath);
            }
            SetChatVisibilityIfReady();
        }

        private void SaveAppConfig()
        {
            try
            {
                File.WriteAllText(configFilePath, JsonSerializer.Serialize(appConfig, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { /* Fails silently */ }
        }

        private void LoadStoresButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "DevGPT files (*.devgpt)|*.devgpt|All files (*.*)|*.*",
                Title = "Select stores.devgpt"
            };
            if (dlg.ShowDialog() == true)
            {
                storesFilePath = dlg.FileName;
                appConfig.StoresFile = storesFilePath;
                SaveAppConfig();
                TryLoadStoresFromFile(storesFilePath);
                SetChatVisibilityIfReady();
            }
        }

        private void TryLoadStoresFromFile(string filePath)
        {
            try
            {
                var rawTxt = File.ReadAllText(filePath);
                StoresDevGPTEditor.Text = rawTxt;
                parsedStores = DevGPTStoreConfigParser.Parse(rawTxt);
                storesDevGPTRaw = rawTxt;
                storesLoaded = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error reading stores file: " + ex.Message);
                storesLoaded = false;
            }
        }

        private void LoadAgentsButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "DevGPT files (*.devgpt)|*.devgpt|All files (*.*)|*.*",
                Title = "Select agents.devgpt"
            };
            if (dlg.ShowDialog() == true)
            {
                agentsFilePath = dlg.FileName;
                appConfig.AgentsFile = agentsFilePath;
                SaveAppConfig();
                TryLoadAgentsFromFile(agentsFilePath);
                SetChatVisibilityIfReady();
            }
        }

        private void TryLoadAgentsFromFile(string filePath)
        {
            try
            {
                var rawTxt = File.ReadAllText(filePath);
                AgentsDevGPTEditor.Text = rawTxt;
                parsedAgents = DevGPTAgentConfigParser.Parse(rawTxt);
                agentsDevGPTRaw = rawTxt;

                FillAgentsAndFlows();

                agentsLoaded = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error reading agents file: " + ex.Message);
                agentsLoaded = false;
            }
        }

    private void FillAgentsAndFlows()
    {
        List<string> agents = parsedAgents.Select(a => $"AGENT: {a.Name}").ToList();
        List<string> flows = parsedFlows.Select(f => $"FLOW: {f.Name}").ToList();
        AgentsAndFlows = agents.Concat(flows).ToList();
    }

    private void LoadFlowsButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "DevGPT files (*.devgpt)|*.devgpt|All files (*.*)|*.*",
                Title = "Select flows.devgpt"
            };
            if (dlg.ShowDialog() == true)
            {
                flowsFilePath = dlg.FileName;
                appConfig.FlowsFile = flowsFilePath;
                SaveAppConfig();
                TryLoadFlowsFromFile(flowsFilePath);
                SetChatVisibilityIfReady();
            }
        }

        private void TryLoadFlowsFromFile(string filePath)
        {
            try
            {
                var rawTxt = File.ReadAllText(filePath);
                FlowsDevGPTEditor.Text = rawTxt;
                parsedFlows = DevGPTFlowConfigParser.Parse(rawTxt);
                flowsDevGPTRaw = rawTxt;
                FillAgentsAndFlows();
                flowsLoaded = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error reading flows file: " + ex.Message);
                flowsLoaded = false;
            }
        }

        private void SaveStoresButton_Click(object sender, RoutedEventArgs e)
        {
            var saveDlg = new SaveFileDialog
            {
                Filter = "DevGPT files (*.devgpt)|*.devgpt|All files (*.*)|*.*",
                Title = "Save stores.devgpt",
                FileName = storesFilePath != null ? Path.GetFileName(storesFilePath) : "stores.devgpt",
                InitialDirectory = storesFilePath != null ? Path.GetDirectoryName(storesFilePath) : null,
                OverwritePrompt = true
            };
            if (saveDlg.ShowDialog() == true)
            {
                var filePath = saveDlg.FileName;
                try
                {
                    var data = DevGPTStoreConfigParser.Parse(StoresDevGPTEditor.Text);
                    var output = DevGPTStoreConfigParser.Serialize(data);
                    File.WriteAllText(filePath, output);
                    storesFilePath = filePath;
                    appConfig.StoresFile = storesFilePath;
                    SaveAppConfig();
                    parsedStores = data;
                    System.Windows.MessageBox.Show($"stores configuration saved.\nPath and filename: {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error saving: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveAgentsButton_Click(object sender, RoutedEventArgs e)
        {
            var saveDlg = new SaveFileDialog
            {
                Filter = "DevGPT files (*.devgpt)|*.devgpt|All files (*.*)|*.*",
                Title = "Save agents.devgpt",
                FileName = agentsFilePath != null ? Path.GetFileName(agentsFilePath) : "agents.devgpt",
                InitialDirectory = agentsFilePath != null ? Path.GetDirectoryName(agentsFilePath) : null,
                OverwritePrompt = true
            };
            if (saveDlg.ShowDialog() == true)
            {
                var filePath = saveDlg.FileName;
                try
                {
                    var data = DevGPTAgentConfigParser.Parse(AgentsDevGPTEditor.Text);
                    var output = DevGPTAgentConfigParser.Serialize(data);
                    File.WriteAllText(filePath, output);
                    agentsFilePath = filePath;
                    appConfig.AgentsFile = agentsFilePath;
                    SaveAppConfig();
                    parsedAgents = data;
                    System.Windows.MessageBox.Show($"Agent config saved.\nPath and filename: {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error saving: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveFlowsButton_Click(object sender, RoutedEventArgs e)
        {
            var saveDlg = new SaveFileDialog
            {
                Filter = "DevGPT files (*.devgpt)|*.devgpt|All files (*.*)|*.*",
                Title = "Save flows.devgpt",
                FileName = flowsFilePath != null ? Path.GetFileName(flowsFilePath) : "flows.devgpt",
                InitialDirectory = flowsFilePath != null ? Path.GetDirectoryName(flowsFilePath) : null,
                OverwritePrompt = true
            };
            if (saveDlg.ShowDialog() == true)
            {
                var filePath = saveDlg.FileName;
                try
                {
                    var data = DevGPTFlowConfigParser.Parse(FlowsDevGPTEditor.Text);
                    var output = DevGPTFlowConfigParser.Serialize(data);
                    File.WriteAllText(filePath, output);
                    flowsFilePath = filePath;
                    appConfig.FlowsFile = flowsFilePath;
                    SaveAppConfig();
                    parsedFlows = data;
                    System.Windows.MessageBox.Show($"Flow config saved.\nPath and filename: {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error saving: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void StoresDevGPTEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressStoresEditorSync) return;
            try { suppressStoresEditorSync = true; parsedStores = DevGPTStoreConfigParser.Parse(StoresDevGPTEditor.Text); storesDevGPTRaw = StoresDevGPTEditor.Text; } finally { suppressStoresEditorSync = false; }
        }

        private void AgentsDevGPTEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressAgentsEditorSync) return;
            try { suppressAgentsEditorSync = true; parsedAgents = DevGPTAgentConfigParser.Parse(AgentsDevGPTEditor.Text); agentsDevGPTRaw = AgentsDevGPTEditor.Text; } finally { suppressAgentsEditorSync = false; }
        }

        private void FlowsDevGPTEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressFlowsEditorSync) return;
            try { suppressFlowsEditorSync = true; parsedFlows = DevGPTFlowConfigParser.Parse(FlowsDevGPTEditor.Text); flowsDevGPTRaw = FlowsDevGPTEditor.Text; } finally { suppressFlowsEditorSync = false; }
        }

        private void SetChatVisibilityIfReady()
        {
            IsChatVisible = agentsLoaded && storesLoaded;
        }

        private bool _openChatDebounce = false;

        private async void NewChatWindowButton_Click(object sender, RoutedEventArgs e)
        {
            // Debounce: voorkom dat snel dubbelklikken tot een dubbele actie leidt
            if (_openChatDebounce) return;
            _openChatDebounce = true;

            // Schakel de chat open-knop direct uit om meervoudige klikken te voorkomen; knop wordt hersteld bij sluiten van het venster of een fout.
            IsOpenChatButtonEnabled = false;

            // Toon direct de laad-animatie zodat de gebruiker direct ziet dat er iets gebeurt (en niet pas na zware disk/config-io)
            IsOpeningChat = true;
            await Task.Yield(); // Forceer UI render van animatie v+�+�r langzame disk-operaties (anders zien gebruikers soms de animatie te laat!)

            // Het inlezen van OpenAI/Google config kan op sommige systemen trage disk I/O veroorzaken (b.v. als netwerk-drive, USB, virusscanner, etc.)
            // - Dit blokkeerde in eerdere versies de zichtbaarheid van de laad-animatie: de animatie kwam pas na de disk-IO!
            // - Daarom laden we deze configs nu asynchroon, v+�+�r de rest van de logica, ZODAT de animatie altijd zichtbaar is v+�+�rdat prijzige disk reads starten.
            GoogleConfig googleSettings = null;
            OpenAIConfig openAISettings = null;
            try
            {
                googleSettings = await Task.Run(() => GoogleConfig.Load());
                openAISettings = await Task.Run(() => OpenAIConfig.Load());
            }
            catch (Exception ex)
            {
                IsOpeningChat = false;
                IsOpenChatButtonEnabled = true;
                _openChatDebounce = false;
                System.Windows.MessageBox.Show("Failed to load configuration: " + ex.Message);
                return;
            }

            try
            {
                const string LogFilePath = @"C:\\Projects\\devgpt\\log";
                string openAIApiKey = openAISettings.ApiKey;
                var storesJson = JsonSerializer.Serialize(parsedStores, new JsonSerializerOptions { WriteIndented = true });
                var agentsJson = JsonSerializer.Serialize(parsedAgents, new JsonSerializerOptions { WriteIndented = true });
                var flowsJson = JsonSerializer.Serialize(parsedFlows, new JsonSerializerOptions { WriteIndented = true });

                var agentManager = new AgentManager(
                    storesJson,
                    agentsJson,
                    flowsJson,
                    openAIApiKey,
                    LogFilePath,
                    true,
                    googleSettings.ProjectId
                );
                await agentManager.LoadStoresAndAgents();

                var newChatWindow = new DevGPT.ChatShared.ChatWindow(new ChatControllerAgentManager(agentManager));
                newChatWindow.AgentOrFlow = SelectedAgentOrFlow;
                newChatWindow.Owner = this;
                newChatWindow.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Failed to open chat window: " + ex.Message);
            }
            finally
            {
                // Zet beide indicatoren weer netjes terug zodat de gebruiker opnieuw kan klikken.
                IsOpeningChat = false;
                IsOpenChatButtonEnabled = true;
                _openChatDebounce = false;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWin = new SettingsWindow();
            settingsWin.Owner = this;
            settingsWin.ShowDialog();
        }
    

        private void ShowFlowsAsCardsButton_Click(object sender, RoutedEventArgs e)
        {
            if (parsedFlows == null) return;
            // Maak een kopie zodat wijzigingen pas bij opslaan worden doorgevoerd
            var cards = new System.Collections.ObjectModel.ObservableCollection<FlowCardModel>(
                parsedFlows.Select(f => new FlowCardModel
                {
                    Name = f.Name,
                    Description = f.Description,
                    CallsAgents = new System.Collections.ObjectModel.ObservableCollection<string>(f.CallsAgents ?? new List<string>())
                })
            );
            // Alle agent-namen ophalen (voor toevoegen vanuit ComboBox)
            var agentNames = parsedAgents?.Select(a => a.Name)?.Distinct()?.OrderBy(x => x)?.ToList() ?? new List<string>();
            var model = new FlowCardsBindingModel { Cards = cards, AllAgents = agentNames };
            model.HookCardPropertyChangedHandlers();
            var dlg = new FlowsCardsWindow(model) { Owner = this };
            var result = dlg.ShowDialog();
            if (result == true && dlg.ResultFlows != null)
            {
                // Sla op in flows
                parsedFlows = dlg.ResultFlows;
                FlowsDevGPTEditor.Text = DevGPTFlowConfigParser.Serialize(parsedFlows);
                flowsDevGPTRaw = FlowsDevGPTEditor.Text;
                // Wordt als normaal behandeld (eventueel vind je verderop een File.Write zoals SaveFlowsButton_Click)
            }
        }

        private AgentCardModel DeepCopyAgentToCard(AgentConfig agent)
        {
            return new AgentCardModel
            {
                Name = agent.Name,
                Description = agent.Description,
                Prompt = agent.Prompt,
                ExplicitModify = agent.ExplicitModify,
                Stores = new ObservableCollection<StoreRef>(
                    agent.Stores.Select(s => new StoreRef { Name = s.Name, Write = s.Write })),
                Functions = new ObservableCollection<string>((agent.Functions ?? new List<string>()).ToList()),
                CallsAgents = new ObservableCollection<string>((agent.CallsAgents ?? new List<string>()).ToList()),
                CallsFlows = new ObservableCollection<string>((agent.CallsFlows ?? new List<string>()).ToList())
            };
        }

        private AgentConfig ConvertCardToAgentConfig(AgentCardModel card)
        {
            return new AgentConfig
            {
                Name = card.Name,
                Description = card.Description,
                Prompt = card.Prompt,
                ExplicitModify = card.ExplicitModify,
                Stores = card.Stores?.Select(s => new StoreRef { Name = s.Name, Write = s.Write }).ToList() ?? new List<StoreRef>(),
                Functions = card.Functions?.ToList() ?? new List<string>(),
                CallsAgents = card.CallsAgents?.ToList() ?? new List<string>(),
                CallsFlows = card.CallsFlows?.ToList() ?? new List<string>()
            };
        }

        private void ShowAgentsAsCardsButton_Click(object sender, RoutedEventArgs e)
        {
            // Zet parsedAgents om naar AgentCardModel
            var cardList = new ObservableCollection<AgentCardModel>(
                (parsedAgents ?? new List<AgentConfig>()).Select(DeepCopyAgentToCard));

            var allStores = new HashSet<string>();
            var allFunctions = new HashSet<string>();
            var allAgents = new HashSet<string>();
            var allFlows = new HashSet<string>();

            foreach (var agent in parsedAgents)
            {
                foreach (var store in agent.Stores ?? Enumerable.Empty<StoreRef>())
                    if (!string.IsNullOrEmpty(store.Name)) allStores.Add(store.Name);
                foreach (var fn in agent.Functions ?? Enumerable.Empty<string>())
                    if (!string.IsNullOrEmpty(fn)) allFunctions.Add(fn);
                if (!string.IsNullOrEmpty(agent.Name)) allAgents.Add(agent.Name);
                foreach (var flow in agent.CallsFlows ?? Enumerable.Empty<string>())
                    if (!string.IsNullOrEmpty(flow)) allFlows.Add(flow);
            }

            var model = new AgentsCardsBindingModel
            {
                Cards = cardList,
                AllStores = allStores.ToList(),
                AllFunctions = allFunctions.ToList(),
                AllAgents = allAgents.ToList(),
                AllFlows = allFlows.ToList()
            };

            var dlg = new AgentsCardsWindow(model);
            dlg.Owner = this;
            var show = dlg.ShowDialog();
            if (show == true && dlg.ResultAgents != null)
            {
                // Zet cards terug naar List<AgentConfig>
                // (Let op: cards na evt. bewerking)
                parsedAgents = dlg.ResultAgents;
                // Eventueel: update editor-weergave of bindings als nodig
                AgentsDevGPTEditor.Text = DevGPTAgentConfigParser.Serialize(parsedAgents);
                agentsDevGPTRaw = AgentsDevGPTEditor.Text;
            }
        }

        // === Stores als kaarten knop: nieuw ===
        private void ShowStoresAsCardsButton_Click(object sender, RoutedEventArgs e)
        {
            // Maak een ObservableCollection<StoreCardModel> op basis van parsedStores
            var cards = new ObservableCollection<StoreCardModel>(
                (parsedStores ?? new List<StoreConfig>()).Select(s => new StoreCardModel
                {
                    Name = s.Name,
                    Description = s.Description,
                    Path = s.Path,
                    SubDirectory = s.SubDirectory,
                    ExcludePattern = string.Join(",", s.ExcludePattern),
                    FileFilters = string.Join(",", s.FileFilters)
                }));

            var model = new StoresCardsBindingModel { Cards = cards };
            var dlg = new StoresCardsWindow(model) { Owner = this };
            var result = dlg.ShowDialog();
            if (result == true && dlg.ResultStores != null)
            {
                parsedStores = dlg.ResultStores;
                // Update de raw editor met de nieuwe serialized stores
                StoresDevGPTEditor.Text = DevGPTStoreConfigParser.Serialize(parsedStores);
                storesDevGPTRaw = StoresDevGPTEditor.Text;
            }
        }

    }

