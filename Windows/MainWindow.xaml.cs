using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows.Controls;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace DevGPT
{
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

        private bool _isChatVisible = false;
        public bool IsChatVisible
        {
            get => _isChatVisible;
            set { _isChatVisible = value; OnPropertyChanged(nameof(IsChatVisible)); }
        }

        private bool _isOpeningChat = false;
        public bool IsOpeningChat
        {
            get => _isOpeningChat;
            set { _isOpeningChat = value; OnPropertyChanged(nameof(IsOpeningChat)); }
        }

        private UserAppConfig appConfig;
        private string configFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appconfig.json");

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
                MessageBox.Show("Error reading stores file: " + ex.Message);
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
                agentsLoaded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading agents file: " + ex.Message);
                agentsLoaded = false;
            }
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
                flowsLoaded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading flows file: " + ex.Message);
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
                    MessageBox.Show($"stores configuration saved.\nPath and filename: {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show($"Agent config saved.\nPath and filename: {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show($"Flow config saved.\nPath and filename: {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async void NewChatWindowButton_Click(object sender, RoutedEventArgs e)
        {
            // Mark as opening chat window
            IsOpeningChat = true;
            try
            {
                const string LogFilePath = @"C:\\Projects\\devgpt\\log";
                var googleSettings = GoogleConfig.Load();
                var openAISettings = OpenAIConfig.Load();
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

                var newChatWindow = new ChatWindow(agentManager);
                newChatWindow.Owner = this;
                newChatWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open chat window: " + ex.Message);
            }
            finally
            {
                IsOpeningChat = false;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWin = new SettingsWindow();
            settingsWin.Owner = this;
            settingsWin.ShowDialog();
        }
    }

    public class UserAppConfig
    {
        public string StoresFile { get; set; } = null;
        public string AgentsFile { get; set; } = null;
        public string FlowsFile { get; set; } = null;
    }
}
