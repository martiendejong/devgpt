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
using Orientation = System.Windows.Controls.Orientation;

namespace DevGPT
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string agentsFilePath = null;
        private string _agentsJsonRaw = string.Empty;
        private string _agentsDevGPTRaw = string.Empty;
        private string lastSavedAgentsPath = null;
        private bool agentsLoaded = false;
        private string agentsLoadedFormat = "json"; // Track last format

        private enum EditorMode { Json, Form, DevGpt }

        private EditorMode _agentsEditorMode = EditorMode.Json;

        private EditorMode _storesEditorMode = EditorMode.Json;

        private string storesJsonRaw = string.Empty;
        private string storesDevGPTRaw = string.Empty;
        private string storesFilePath = null;
        private string lastSavedStoresPath = null;
        private bool storesLoaded = false;
        private List<StoreConfig> _parsedStores = new List<StoreConfig>();
        private List<AgentConfig> _parsedAgents = new List<AgentConfig>();

        private string _lastDevGPTText = "";
        private string _lastJsonText = "";
        private bool _suppressEditorSync = false;

        // NEW: Per-type suppressors for realtime bidirectional syncing
        private bool _suppressStoresEditorSync = false;
        private bool _suppressAgentsEditorSync = false;

        private bool _isChatVisible = false;
        public bool IsChatVisible
        {
            get => _isChatVisible;
            set { _isChatVisible = value; OnPropertyChanged(nameof(IsChatVisible)); }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            StoresJsonEditor.TextChanged += StoresJsonEditor_TextChanged;
            StoresDevGPTEditor.TextChanged += StoresDevGPTEditor_TextChanged;
            StoresJsonEditor.TextChanged += StoresJsonEditor_SyncRealtime;
            StoresDevGPTEditor.TextChanged += StoresDevGPTEditor_SyncRealtime;
            UpdateStoresEditorContent();
            AgentsJsonEditor.TextChanged += AgentsJsonEditor_TextChanged;
            AgentsDevGPTEditor.TextChanged += AgentsDevGPTEditor_TextChanged;
            AgentsJsonEditor.TextChanged += AgentsJsonEditor_SyncRealtime;
            AgentsDevGPTEditor.TextChanged += AgentsDevGPTEditor_SyncRealtime;
            UpdateAgentsEditorContent();
            SetChatVisibilityIfReady();
        }

        private string SelectedStoreFormat =>
            (StoreFormatComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag) ? tag : "json";

        private void LoadStoresButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Stores config (.json, .devgpt)|*.json;*.devgpt|JSON files (*.json)|*.json|DevGPT files (*.devgpt)|*.devgpt|All files (*.*)|*.*",
                Title = "Select stores.json or .devgpt"
            };
            if (dlg.ShowDialog() == true)
            {
                storesFilePath = dlg.FileName;
                try
                {
                    var rawTxt = File.ReadAllText(storesFilePath);
                    // Detect format (.json or .devgpt)
                    if (StoreConfigFormatHelper.IsLikelyJson(rawTxt) || storesFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        StoresJsonEditor.Text = rawTxt;
                        _parsedStores = JsonSerializer.Deserialize<List<StoreConfig>>(rawTxt, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<StoreConfig>();
                        storesJsonRaw = rawTxt;
                        storesDevGPTRaw = string.Empty;
                    }
                    else
                    {
                        StoresDevGPTEditor.Text = rawTxt;
                        _parsedStores = DevGPTStoreConfigParser.Parse(rawTxt);
                        storesDevGPTRaw = rawTxt;
                        storesJsonRaw = string.Empty;
                    }
                    storesLoaded = true;
                    lastSavedStoresPath = storesFilePath;
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
                Filter = "Agents config (.json, .devgpt)|*.json;*.devgpt|JSON files (*.json)|*.json|DevGPT files (*.devgpt)|*.devgpt|All files (*.*)|*.*",
                Title = "Select agents.json or .devgpt"
            };
            if (dlg.ShowDialog() == true)
            {
                agentsFilePath = dlg.FileName;
                try
                {
                    var rawTxt = File.ReadAllText(agentsFilePath);
                    // Detect format (.json or .devgpt)
                    if (AgentConfigFormatHelper.IsLikelyJson(rawTxt) || agentsFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        agentsLoadedFormat = "json";
                        AgentsJsonEditor.Text = rawTxt;
                        _parsedAgents = JsonSerializer.Deserialize<List<AgentConfig>>(rawTxt, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<AgentConfig>();
                        _agentsJsonRaw = rawTxt;
                        _agentsDevGPTRaw = string.Empty;
                    }
                    else
                    {
                        agentsLoadedFormat = "devgpt";
                        AgentsDevGPTEditor.Text = rawTxt;
                        _parsedAgents = DevGPTAgentConfigParser.Parse(rawTxt);
                        _agentsDevGPTRaw = rawTxt;
                        _agentsJsonRaw = string.Empty;
                    }
                    agentsLoaded = true;
                    lastSavedAgentsPath = agentsFilePath;
                    SetChatVisibilityIfReady();
                    UpdateAgentsEditorContent();
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
                string format = SelectedStoreFormat;
                if (_storesEditorMode == EditorMode.DevGpt)
                {
                    var data = DevGPTStoreConfigParser.Parse(StoresDevGPTEditor.Text);
                    MessageBox.Show("Valid .devgpt format", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (format == "devgpt")
                {
                    var data = DevGPTStoreConfigParser.Parse(AgentsJsonEditor.Text);
                    MessageBox.Show("Valid .devgpt format", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var data = JsonSerializer.Deserialize<List<StoreConfig>>(AgentsJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    MessageBox.Show("Valid JSON", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid format: " + ex.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ValidateAgentsJsonButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate based on current mode
            try
            {
                List<AgentConfig> testAgents = null;
                if (_agentsEditorMode == EditorMode.Json)
                    testAgents = JsonSerializer.Deserialize<List<AgentConfig>>(AgentsJsonEditor.Text);
                else if (_agentsEditorMode == EditorMode.DevGpt)
                    testAgents = DevGPTAgentConfigParser.Parse(AgentsDevGPTEditor.Text);
                else // Form mode
                    testAgents = _parsedAgents;
                MessageBox.Show($"Valid. Loaded {testAgents?.Count ?? 0} agents.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid agent configuration: " + ex.Message);
            }
        }

        private void SaveStoresButton_Click(object sender, RoutedEventArgs e)
        {
            string format = SelectedStoreFormat;
            string filter = format == "devgpt"
                ? "DevGPT files (*.devgpt)|*.devgpt|JSON files (*.json)|*.json|Alle ondersteunde bestanden (*.devgpt;*.json)|*.devgpt;*.json"
                : "JSON files (*.json)|*.json|DevGPT files (*.devgpt)|*.devgpt|Alle ondersteunde bestanden (*.devgpt;*.json)|*.devgpt;*.json";

            string suggestedName = !string.IsNullOrWhiteSpace(lastSavedStoresPath) ? Path.GetFileName(lastSavedStoresPath) : (format == "devgpt" ? "stores.devgpt" : "stores.json");
            var saveDlg = new SaveFileDialog
            {
                Filter = filter,
                Title = format == "devgpt" ? "Save stores.devgpt" : "Save stores.json",
                FileName = suggestedName,
                InitialDirectory = !string.IsNullOrWhiteSpace(lastSavedStoresPath) ? Path.GetDirectoryName(lastSavedStoresPath) : null,
                OverwritePrompt = true
            };
            if (saveDlg.ShowDialog() == true)
            {
                string filePath = saveDlg.FileName;
                try
                {
                    var data = GetStoresConfigFormData();
                    SaveStoresConfig(format, filePath, data);

                    _parsedStores = data;
                    // Resync editors after save
                    _lastDevGPTText = DevGPTStoreConfigParser.Serialize(data);
                    _lastJsonText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    StoresDevGPTEditor.Text = _lastDevGPTText;
                    StoresJsonEditor.Text = _lastJsonText;
                    storesFilePath = filePath;
                    lastSavedStoresPath = filePath;
                    MessageBox.Show($"stores configuration saved.\nPath and filename: {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (UnauthorizedAccessException uae)
                {
                    MessageBox.Show($"Kan niet opslaan: u heeft geen rechten tot deze locatie.\n{uae.Message}", "Opslaan mislukt", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (IOException ioe)
                {
                    MessageBox.Show($"Fout bij bestandsbewerking:\n{ioe.Message}", "Opslaan mislukt", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Onbekende fout bij opslaan:\n{ex.Message}", "Opslaan mislukt", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveStoresConfig(string format, string filePath, List<StoreConfig> data)
        {
            string output;
            if (_storesEditorMode == EditorMode.DevGpt || format == "devgpt" || filePath.EndsWith(".devgpt", StringComparison.OrdinalIgnoreCase))
                output = DevGPTStoreConfigParser.Serialize(data);
            else
                output = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, output);
        }

        private List<StoreConfig> GetStoresConfigFormData()
        {
            List<StoreConfig> data;
            if (_storesEditorMode == EditorMode.DevGpt)
            {
                data = DevGPTStoreConfigParser.Parse(StoresDevGPTEditor.Text);
            }
            else if (_storesEditorMode == EditorMode.Json)
            {
                data = JsonSerializer.Deserialize<List<StoreConfig>>(StoresJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            else // form
                data = _parsedStores;
            return data;
        }

        private void SaveAgentsButton_Click(object sender, RoutedEventArgs e)
        {
            var saveDlg = new SaveFileDialog
            {
                Filter = "Agents config (.json, .devgpt)|*.json;*.devgpt|JSON files (*.json)|*.json|DevGPT files (*.devgpt)|*.devgpt|All files (*.*)|*.*",
                Title = "Save agents.json/.devgpt",
                FileName = !string.IsNullOrWhiteSpace(lastSavedAgentsPath) ? Path.GetFileName(lastSavedAgentsPath) : "agents.json",
                InitialDirectory = !string.IsNullOrWhiteSpace(lastSavedAgentsPath) ? Path.GetDirectoryName(lastSavedAgentsPath) : null,
                OverwritePrompt = true
            };
            if (saveDlg.ShowDialog() == true)
            {
                string filePath = saveDlg.FileName;
                try
                {
                    List<AgentConfig> data = null;
                    string output = null;

                    data = GetAgentsConfigFormData();
                    SaveAgentsConfigData(filePath, data);

                    _parsedAgents = data;
                    agentsFilePath = filePath;
                    lastSavedAgentsPath = filePath;
                    MessageBox.Show($"Agent config opgeslagen.\nPad en bestandsnaam: {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateAgentsEditorContent();
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

        private static void SaveAgentsConfigData(string filePath, List<AgentConfig> data)
        {
            string output;
            // Choose the serialization logic
            if (filePath.EndsWith(".devgpt", StringComparison.OrdinalIgnoreCase))
            {
                output = DevGPTAgentConfigParser.Serialize(data);
            }
            else // Default to JSON
            {
                output = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            }
            File.WriteAllText(filePath, output);
        }

        private List<AgentConfig> GetAgentsConfigFormData()
        {
            List<AgentConfig> data;
            if (_agentsEditorMode == EditorMode.Form)
            {
                data = _parsedAgents;
            }
            else if (_agentsEditorMode == EditorMode.DevGpt)
            {
                // Parse DevGPT
                data = DevGPTAgentConfigParser.Parse(AgentsDevGPTEditor.Text);
            }
            else // Text (JSON)
            {
                data = JsonSerializer.Deserialize<List<AgentConfig>>(AgentsJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            return data;
        }

        private void StoresJsonEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_storesEditorMode != EditorMode.Json) return;
            try
            {
                var stores = JsonSerializer.Deserialize<List<StoreConfig>>(StoresJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _parsedStores = stores ?? new List<StoreConfig>();
                storesJsonRaw = StoresJsonEditor.Text;
            }
            catch { }
        }

        private void StoresDevGPTEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_storesEditorMode != EditorMode.DevGpt) return;
            // Possible: parse or preview .devgpt content
            try { _parsedStores = DevGPTStoreConfigParser.Parse(StoresDevGPTEditor.Text); storesDevGPTRaw = StoresDevGPTEditor.Text; } catch { }
        }

        // ---- ADDITION: Real-time bidirectional syncing logic for Stores/Agents below ----

        // STORES: JSON to devgpt
        private void StoresJsonEditor_SyncRealtime(object sender, TextChangedEventArgs e)
        {
            if (_storesEditorMode != EditorMode.Json) return;
            if (_suppressStoresEditorSync) return;
            try
            {
                _suppressStoresEditorSync = true;
                var stores = JsonSerializer.Deserialize<List<StoreConfig>>(StoresJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (stores != null)
                {
                    var devgptText = DevGPTStoreConfigParser.Serialize(stores);
                    if (StoresDevGPTEditor.Text != devgptText)
                        StoresDevGPTEditor.Text = devgptText;
                }
            }
            catch { }
            finally { _suppressStoresEditorSync = false; }
        }

        // STORES: devgpt to JSON
        private void StoresDevGPTEditor_SyncRealtime(object sender, TextChangedEventArgs e)
        {
            if (_storesEditorMode != EditorMode.DevGpt) return;
            if (_suppressStoresEditorSync) return;
            try
            {
                _suppressStoresEditorSync = true;
                var stores = DevGPTStoreConfigParser.Parse(StoresDevGPTEditor.Text);
                if (stores != null)
                {
                    var jsonText = JsonSerializer.Serialize(stores, new JsonSerializerOptions { WriteIndented = true });
                    if (StoresJsonEditor.Text != jsonText)
                        StoresJsonEditor.Text = jsonText;
                }
            }
            catch { }
            finally { _suppressStoresEditorSync = false; }
        }

        // AGENTS: JSON to devgpt
        private void AgentsJsonEditor_SyncRealtime(object sender, TextChangedEventArgs e)
        {
            if (_agentsEditorMode != EditorMode.Json) return;
            if (_suppressAgentsEditorSync) return;
            try
            {
                _suppressAgentsEditorSync = true;
                var agents = JsonSerializer.Deserialize<List<AgentConfig>>(AgentsJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (agents != null)
                {
                    var devgptText = DevGPTAgentConfigParser.Serialize(agents);
                    if (AgentsDevGPTEditor.Text != devgptText)
                        AgentsDevGPTEditor.Text = devgptText;
                }
            }
            catch { }
            finally { _suppressAgentsEditorSync = false; }
        }

        // AGENTS: devgpt to JSON
        private void AgentsDevGPTEditor_SyncRealtime(object sender, TextChangedEventArgs e)
        {
            if (_agentsEditorMode != EditorMode.DevGpt) return;
            if (_suppressAgentsEditorSync) return;
            try
            {
                _suppressAgentsEditorSync = true;
                var agents = DevGPTAgentConfigParser.Parse(AgentsDevGPTEditor.Text);
                if (agents != null)
                {
                    var jsonText = JsonSerializer.Serialize(agents, new JsonSerializerOptions { WriteIndented = true });
                    if (AgentsJsonEditor.Text != jsonText)
                        AgentsJsonEditor.Text = jsonText;
                }
            }
            catch { }
            finally { _suppressAgentsEditorSync = false; }
        }

        // ---- END OF REALTIME SYNC LOGIC ----

        private void UpdateStoresEditorContent()
        {
            if (StoresEditorContent == null) return;
            if (_storesEditorMode == EditorMode.Json)
            {
                StoresJsonEditor.Visibility = Visibility.Visible;
                StoresDevGPTEditor.Visibility = Visibility.Collapsed;
                StoresEditorContent.Visibility = Visibility.Collapsed;
                StoresEditorContent.Content = null;
            }
            else if (_storesEditorMode == EditorMode.DevGpt)
            {
                StoresJsonEditor.Visibility = Visibility.Collapsed;
                StoresDevGPTEditor.Visibility = Visibility.Visible;
                StoresEditorContent.Visibility = Visibility.Collapsed;
                StoresEditorContent.Content = null;
            }
            else
            {
                StoresJsonEditor.Visibility = Visibility.Collapsed;
                StoresDevGPTEditor.Visibility = Visibility.Collapsed;
                StoresEditorContent.Visibility = Visibility.Visible;
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

        private void SetChatVisibilityIfReady()
        {
            // Chat visible if both agentsLoaded and storesLoaded
            IsChatVisible = agentsLoaded && storesLoaded;
        }

        private void RadioEditorMode_Checked(object sender, RoutedEventArgs e)
        {
            if (RadioTextEditor != null && (RadioTextEditor.IsChecked ?? false))
            {
                _storesEditorMode = EditorMode.Json;
                if (!string.IsNullOrEmpty(storesJsonRaw))
                    StoresJsonEditor.Text = storesJsonRaw;
            }
            else if (RadioFormEditor != null && (RadioFormEditor.IsChecked ?? false))
            {
                _storesEditorMode = EditorMode.Form;
                // No raw assignment needed
            }
            else
            {
                _storesEditorMode = EditorMode.DevGpt;
                if (!string.IsNullOrEmpty(storesDevGPTRaw))
                    StoresDevGPTEditor.Text = storesDevGPTRaw;
            }
            UpdateStoresEditorContent();
        }

        private void RadioAgentEditorMode_Checked(object sender, RoutedEventArgs e)
        {
            if (RadioAgentsTextEditor != null && (RadioAgentsTextEditor.IsChecked ?? false))
            {
                _agentsEditorMode = EditorMode.Json;
                if (!string.IsNullOrEmpty(_agentsJsonRaw))
                    AgentsJsonEditor.Text = _agentsJsonRaw;
            }
            else if (RadioAgentsFormEditor != null && (RadioAgentsFormEditor.IsChecked ?? false))
            {
                _agentsEditorMode = EditorMode.Form;
            }
            else
            {
                _agentsEditorMode = EditorMode.DevGpt;
                if (!string.IsNullOrEmpty(_agentsDevGPTRaw))
                    AgentsDevGPTEditor.Text = _agentsDevGPTRaw;
            }
            UpdateAgentsEditorContent();
        }

        private void UpdateAgentsEditorContent()
        {
            if (AgentsEditorContent == null) return;
            if (_agentsEditorMode == EditorMode.Json)
            {
                AgentsJsonEditor.Visibility = Visibility.Visible;
                AgentsDevGPTEditor.Visibility = Visibility.Collapsed;
                AgentsEditorContent.Visibility = Visibility.Collapsed;
                AgentsEditorContent.Content = null;
            }
            else if (_agentsEditorMode == EditorMode.DevGpt)
            {
                AgentsJsonEditor.Visibility = Visibility.Collapsed;
                AgentsDevGPTEditor.Visibility = Visibility.Visible;
                AgentsEditorContent.Visibility = Visibility.Collapsed;
                AgentsEditorContent.Content = null;
            }
            else
            {
                AgentsJsonEditor.Visibility = Visibility.Collapsed;
                AgentsDevGPTEditor.Visibility = Visibility.Collapsed;
                AgentsEditorContent.Visibility = Visibility.Visible;
                var sp = new StackPanel { Orientation = Orientation.Vertical };
                if (_parsedAgents == null || _parsedAgents.Count == 0)
                {
                    sp.Children.Add(new TextBlock { Text = "Geen agents gevonden in geladen JSON.", Foreground = System.Windows.Media.Brushes.Red });
                }
                else
                {
                    foreach (var agent in _parsedAgents)
                    {
                        var ctl = new AgentFormEditor(agent);
                        sp.Children.Add(ctl);
                    }
                }
                AgentsEditorContent.Content = sp;
            }
        }

        private async void NewChatWindowButton_Click(object sender, RoutedEventArgs e)
        {
            EnsureLatestJson();

            const string LogFilePath = @"C:\\Projects\\devgpt\\log";
            var googleSettings = GoogleConfig.Load();
            var openAISettings = OpenAIConfig.Load();
            string openAIApiKey = openAISettings.ApiKey;

            var storesData = GetStoresConfigFormData();
            var storesJson = JsonSerializer.Serialize(storesData, new JsonSerializerOptions { WriteIndented = true });
            var agentsData = GetAgentsConfigFormData();
            var agentsJson = JsonSerializer.Serialize(agentsData, new JsonSerializerOptions { WriteIndented = true });

            var agentManager = new AgentManager(
                storesJson,
                agentsJson,
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

        private void AgentsJsonEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_agentsEditorMode != EditorMode.Json) return;
            try
            {
                var agents = JsonSerializer.Deserialize<List<AgentConfig>>(AgentsJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _parsedAgents = agents ?? new List<AgentConfig>();
                _agentsJsonRaw = AgentsJsonEditor.Text;
            }
            catch { }
        }

        private void AgentsDevGPTEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_agentsEditorMode != EditorMode.DevGpt) return;
            try { _parsedAgents = DevGPTAgentConfigParser.Parse(AgentsDevGPTEditor.Text); _agentsDevGPTRaw = AgentsDevGPTEditor.Text; } catch { }
        }

        private void EnsureLatestJson()
        {
            try
            {
                string format = SelectedStoreFormat;
                if (_storesEditorMode == EditorMode.Json)
                {
                    if (_parsedStores == null || _parsedStores.Count == 0)
                    {
                        if (format == "devgpt")
                            _parsedStores = DevGPTStoreConfigParser.Parse(StoresJsonEditor.Text) ?? new List<StoreConfig>();
                        else
                            _parsedStores = JsonSerializer.Deserialize<List<StoreConfig>>(StoresJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<StoreConfig>();
                    }
                    if (_parsedAgents == null || _parsedAgents.Count == 0)
                        _parsedAgents = JsonSerializer.Deserialize<List<AgentConfig>>(AgentsJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<AgentConfig>();
                }
                else if (_storesEditorMode == EditorMode.DevGpt)
                {
                    if (_parsedStores == null || _parsedStores.Count == 0)
                    {
                        _parsedStores = DevGPTStoreConfigParser.Parse(StoresDevGPTEditor.Text) ?? new List<StoreConfig>();
                    }
                    if (_parsedAgents == null || _parsedAgents.Count == 0)
                        _parsedAgents = JsonSerializer.Deserialize<List<AgentConfig>>(AgentsJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<AgentConfig>();
                }
                // otherwise form mode is always synced because of two-way binding per field
            }
            catch { }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWin = new SettingsWindow();
            settingsWin.Owner = this;
            settingsWin.ShowDialog();
        }
    }
}
