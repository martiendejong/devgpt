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
using Orientation = System.Windows.Controls.Orientation; // <- For ContentControl, StackPanel

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

        private string lastSavedAgentsPath;
        private string lastSavedStoresPath;
        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            UpdateStoresEditorContent();
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
                    UpdateStoresEditorContent();
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
                var data = JsonSerializer.Deserialize<List<StoreConfig>>(GetStoresEditorJsonText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
            var saveDlg = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                Title = "Save stores.json",
                FileName = !string.IsNullOrWhiteSpace(lastSavedStoresPath) ? Path.GetFileName(lastSavedStoresPath) : "stores.json",
                InitialDirectory = !string.IsNullOrWhiteSpace(lastSavedStoresPath) ? Path.GetDirectoryName(lastSavedStoresPath) : null,
                OverwritePrompt = true
            };
            if (saveDlg.ShowDialog() == true)
            {
                string filePath = saveDlg.FileName;
                try
                {
                    List<StoreConfig> data;
                    if (_editorMode == EditorMode.Text)
                        data = JsonSerializer.Deserialize<List<StoreConfig>>(StoresJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    else
                        data = _parsedStores;
                    var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(filePath, json);
                    _parsedStores = data;
                    storesFilePath = filePath;
                    lastSavedStoresPath = filePath;
                    MessageBox.Show($"stores.json saved.\nPad en bestandsnaam: {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (UnauthorizedAccessException uae)
                {
                    MessageBox.Show($"Kan niet opslaan: u heeft geen rechten tot deze locatie.\n{uae.Message}", "Opslaan mislukt", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (IOException ioe)
                {
                    MessageBox.Show($"Fout bij bestandsbewerking:\n{ioe.Message}", "Opslaan mislukt", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (JsonException je)
                {
                    MessageBox.Show($"Kan niet opslaan: JSON is ongeldig!\n{je.Message}", "Opslaan mislukt", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Onbekende fout bij opslaan:\n{ex.Message}", "Opslaan mislukt", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveAgentsButton_Click(object sender, RoutedEventArgs e)
        {
            var saveDlg = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                Title = "Save agents.json",
                FileName = !string.IsNullOrWhiteSpace(lastSavedAgentsPath) ? Path.GetFileName(lastSavedAgentsPath) : "agents.json",
                InitialDirectory = !string.IsNullOrWhiteSpace(lastSavedAgentsPath) ? Path.GetDirectoryName(lastSavedAgentsPath) : null,
                OverwritePrompt = true
            };
            if (saveDlg.ShowDialog() == true)
            {
                string filePath = saveDlg.FileName;
                try
                {
                    var data = JsonSerializer.Deserialize<List<AgentConfig>>(AgentsJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(filePath, json);
                    _parsedAgents = data;
                    agentsFilePath = filePath;
                    lastSavedAgentsPath = filePath;
                    MessageBox.Show($"agents.json saved.\nPad en bestandsnaam: {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (UnauthorizedAccessException uae)
                {
                    MessageBox.Show($"Kan niet opslaan: u heeft geen rechten tot deze locatie.\n{uae.Message}", "Opslaan mislukt", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (IOException ioe)
                {
                    MessageBox.Show($"Fout bij bestandsbewerking:\n{ioe.Message}", "Opslaan mislukt", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (JsonException je)
                {
                    MessageBox.Show($"Kan niet opslaan: JSON is ongeldig!\n{je.Message}", "Opslaan mislukt", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Onbekende fout bij opslaan:\n{ex.Message}", "Opslaan mislukt", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private enum EditorMode { Text, Form }
        private EditorMode _editorMode = EditorMode.Text;

        private void RadioEditorMode_Checked(object sender, RoutedEventArgs e)
        {
            if (RadioTextEditor != null && (RadioTextEditor.IsChecked ?? false))
                _editorMode = EditorMode.Text;
            else
                _editorMode = EditorMode.Form;
            UpdateStoresEditorContent();
        }

        private void UpdateStoresEditorContent()
        {
            if (StoresEditorContent == null) return;
            if (_editorMode == EditorMode.Text)
            {
                StoresJsonEditor.Visibility = Visibility.Visible;
                var dummy = new ContentControl { Content = null };
                StoresEditorContent.Content = dummy;
            }
            else if (_editorMode == EditorMode.Form)
            {
                StoresJsonEditor.Visibility = Visibility.Collapsed;
                var sp = new StackPanel { Orientation = Orientation.Vertical };
                if (_parsedStores == null || _parsedStores.Count == 0)
                {
                    sp.Children.Add(new TextBlock { Text = "Geen stores gevonden in geladen JSON.", Foreground = System.Windows.Media.Brushes.Red });
                }
                else
                {
                    foreach (var store in _parsedStores)
                    {
                        var ctl = new StoreFormEditor(store);
                        sp.Children.Add(ctl);
                    }
                }
                StoresEditorContent.Content = sp;
            }
        }

        private string GetStoresEditorJsonText()
        {
            if (_editorMode == EditorMode.Text)
                return StoresJsonEditor.Text;
            else
                return JsonSerializer.Serialize(_parsedStores, new JsonSerializerOptions { WriteIndented = true });
        }

        // --- Nieuw chatvenster openen feature --- //
        private async void NewChatWindowButton_Click(object sender, RoutedEventArgs e)
        {
            EnsureLatestJson();

            const string LogFilePath = @"C:\\Projects\\devgpt\\log";
            var openAISettings = OpenAIConfig.Load();
            string openAIApiKey = openAISettings.ApiKey;

            var agentManager = new AgentManager(
                GetStoresEditorJsonText(),
                AgentsJsonEditor.Text,
                openAIApiKey,
                LogFilePath,
                true
            );
            await agentManager.LoadStoresAndAgents();

            var newChatWindow = new ChatWindow(agentManager);
            newChatWindow.Owner = this;
            newChatWindow.Show();
        }

        private void EnsureLatestJson()
        {
            try
            {
                if (_editorMode == EditorMode.Text)
                {
                    if (_parsedStores == null || _parsedStores.Count == 0)
                        _parsedStores = JsonSerializer.Deserialize<List<StoreConfig>>(StoresJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<StoreConfig>();
                    if (_parsedAgents == null || _parsedAgents.Count == 0)
                        _parsedAgents = JsonSerializer.Deserialize<List<AgentConfig>>(AgentsJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<AgentConfig>();
                }
                // otherwise form mode is always synced because of two-way binding per field
            }
            catch { }
        }

        // SETTINGS BUTTON HANDLER
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWin = new SettingsWindow();
            settingsWin.Owner = this;
            settingsWin.ShowDialog();
        }
    }
}
