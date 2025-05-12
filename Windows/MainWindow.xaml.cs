using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using System.ComponentModel;
// using DevGPT.AgentFactory; // Remove this and add direct file-level classes usage

namespace DevGPT
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string storesFilePath;
        private string agentsFilePath;
        private string _storesJsonRaw;
        private string _agentsJsonRaw;
        private bool storesLoaded = false;
        private bool agentsLoaded = false;
        
        private List<StoreConfig> _parsedStores = new List<StoreConfig>();
        private List<AgentConfig> _parsedAgents = new List<AgentConfig>();

        private bool _isChatVisible = false;
        public bool IsChatVisible
        {
            get => _isChatVisible;
            set {
                _isChatVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChatVisible)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void SetChatVisibilityIfReady()
        {
            if (storesLoaded && agentsLoaded)
                IsChatVisible = true;
        }

        private void LoadStoresButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                Title = "Select stores.json"
            };
            if (dlg.ShowDialog() == true)
            {
                storesFilePath = dlg.FileName;
                try
                {
                    _storesJsonRaw = File.ReadAllText(storesFilePath);
                    StoresJsonEditor.Text = _storesJsonRaw;
                    _parsedStores = JsonSerializer.Deserialize<List<StoreConfig>>(_storesJsonRaw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<StoreConfig>();
                    storesLoaded = true;
                    SetChatVisibilityIfReady();
                    MessageBox.Show("Loaded stores.json: " + storesFilePath, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading file: " + ex.Message);
                }
            }
        }

        private void LoadAgentsButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                Title = "Select agents.json"
            };
            if (dlg.ShowDialog() == true)
            {
                agentsFilePath = dlg.FileName;
                try
                {
                    _agentsJsonRaw = File.ReadAllText(agentsFilePath);
                    AgentsJsonEditor.Text = _agentsJsonRaw;
                    _parsedAgents = JsonSerializer.Deserialize<List<AgentConfig>>(_agentsJsonRaw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<AgentConfig>();
                    agentsLoaded = true;
                    SetChatVisibilityIfReady();
                    MessageBox.Show("Loaded agents.json: " + agentsFilePath, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading file: " + ex.Message);
                }
            }
        }

        private void ValidateStoresJsonButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = JsonSerializer.Deserialize<List<StoreConfig>>(StoresJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                MessageBox.Show("Valid JSON", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid JSON: " + ex.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ValidateAgentsJsonButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = JsonSerializer.Deserialize<List<AgentConfig>>(AgentsJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                MessageBox.Show("Valid JSON", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid JSON: " + ex.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveStoresButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(storesFilePath))
            {
                MessageBox.Show("No stores.json loaded.");
                return;
            }
            // Validate before saving
            try
            {
                var data = JsonSerializer.Deserialize<List<StoreConfig>>(StoresJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(storesFilePath, json);
                _parsedStores = data;
                MessageBox.Show("stores.json saved.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot save: invalid JSON!\n" + ex.Message);
            }
        }

        private void SaveAgentsButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(agentsFilePath))
            {
                MessageBox.Show("No agents.json loaded.");
                return;
            }
            // Validate before saving
            try
            {
                var data = JsonSerializer.Deserialize<List<AgentConfig>>(AgentsJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(agentsFilePath, json);
                _parsedAgents = data;
                MessageBox.Show("agents.json saved.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot save: invalid JSON!\n" + ex.Message);
            }
        }

        // --- Nieuw chatvenster openen feature --- //
        private async void NewChatWindowButton_Click(object sender, RoutedEventArgs e)
        {
            // Defensive: make sure we always have parsed JSON. Otherwise try parsing
            EnsureLatestJson();

            const string LogFilePath = @"C:\Projects\devgpt\log";
            const string StoresJsonPath = "stores.json";
            const string AgentsJsonPath = "agents.json";

            var openAISettings = OpenAIConfig.Load();
            string openAIApiKey = openAISettings.ApiKey;

            var agentManager = new AgentManager(
                StoresJsonEditor.Text,
                AgentsJsonEditor.Text,
                openAIApiKey,
                LogFilePath,
                true
            );
            await agentManager.LoadStoresAndAgents();


            var newChatWindow = new ChatWindow(agentManager);
            newChatWindow.Owner = this; // for UX
            newChatWindow.Show();
        }
        private void EnsureLatestJson()
        {
            try
            {
                if (_parsedStores == null || _parsedStores.Count == 0)
                    _parsedStores = JsonSerializer.Deserialize<List<StoreConfig>>(StoresJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<StoreConfig>();
                if (_parsedAgents == null || _parsedAgents.Count == 0)
                    _parsedAgents = JsonSerializer.Deserialize<List<AgentConfig>>(AgentsJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<AgentConfig>();
            }
            catch { }
        }
    }
}
