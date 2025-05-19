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

// Add required using to resolve DevGPTStoreConfigParser
// Assume StoreConfig and DevGPTStoreConfigParser are accessible in the same assembly or with using

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

        private string _lastDevGPTText = "";
        private string _lastJsonText = "";
        private bool _suppressEditorSync = false;

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
            StoresJsonEditor.TextChanged += StoresJsonEditor_TextChanged;
            StoresDevGPTEditor.TextChanged += StoresDevGPTEditor_TextChanged;
            UpdateStoresEditorContent();
        }

        private string SelectedStoreFormat =>
            (StoreFormatComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag) ? tag : "json";

        private void SetChatVisibilityIfReady()
        {
            if (storesLoaded && agentsLoaded)
                IsChatVisible = true;
        }

        private void LoadStoresButton_Click(object sender, RoutedEventArgs e)
        {
            string filter = "Alle ondersteunde bestanden (*.json;*.devgpt)|*.json;*.devgpt|JSON files (*.json)|*.json|DevGPT files (*.devgpt)|*.devgpt";
            var dlg = new OpenFileDialog
            {
                Filter = filter,
                Title = "Select stores.json/.devgpt"
            };
            if (dlg.ShowDialog() == true)
            {
                storesFilePath = dlg.FileName;
                string extension = Path.GetExtension(storesFilePath).ToLowerInvariant();
                string format = SelectedStoreFormat;
                if (extension == ".devgpt")
                    format = "devgpt";
                else if (extension == ".json")
                    format = "json";
                // Override combo to match file type
                for (int i = 0; i < StoreFormatComboBox.Items.Count; ++i)
                {
                    var item = (ComboBoxItem)StoreFormatComboBox.Items[i];
                    if ((string)item.Tag == format)
                    {
                        StoreFormatComboBox.SelectedIndex = i;
                        break;
                    }
                }
                try
                {
                    _storesJsonRaw = File.ReadAllText(storesFilePath);
                    if (format == "devgpt")
                    {
                        _parsedStores = DevGPTStoreConfigParser.Parse(_storesJsonRaw) ?? new List<StoreConfig>();
                        _lastDevGPTText = _storesJsonRaw;
                        _lastJsonText = JsonSerializer.Serialize(_parsedStores, new JsonSerializerOptions { WriteIndented = true });
                        StoresDevGPTEditor.Text = _lastDevGPTText;
                        StoresJsonEditor.Text = _lastJsonText;
                    }
                    else // json
                    {
                        _parsedStores = JsonSerializer.Deserialize<List<StoreConfig>>(_storesJsonRaw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<StoreConfig>();
                        _lastJsonText = JsonSerializer.Serialize(_parsedStores, new JsonSerializerOptions { WriteIndented = true });
                        _lastDevGPTText = DevGPTStoreConfigParser.Serialize(_parsedStores);
                        StoresJsonEditor.Text = _lastJsonText;
                        StoresDevGPTEditor.Text = _lastDevGPTText;
                    }
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
                string format = SelectedStoreFormat;
                if (_editorMode == EditorMode.DevGptText)
                {
                    var data = DevGPTStoreConfigParser.Parse(StoresDevGPTEditor.Text);
                    MessageBox.Show("Valid .devgpt format", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (format == "devgpt")
                {
                    var data = DevGPTStoreConfigParser.Parse(GetStoresEditorJsonText());
                    MessageBox.Show("Valid .devgpt format", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var data = JsonSerializer.Deserialize<List<StoreConfig>>(GetStoresEditorJsonText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
                    List<StoreConfig> data;
                    if (_editorMode == EditorMode.DevGptText)
                    {
                        data = DevGPTStoreConfigParser.Parse(StoresDevGPTEditor.Text);
                    }
                    else if (_editorMode == EditorMode.Text)
                    {
                        if (format == "devgpt")
                            data = DevGPTStoreConfigParser.Parse(StoresJsonEditor.Text);
                        else
                            data = JsonSerializer.Deserialize<List<StoreConfig>>(StoresJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    else // form
                        data = _parsedStores;

                    string output;
                    if (_editorMode == EditorMode.DevGptText || format == "devgpt" || filePath.EndsWith(".devgpt", StringComparison.OrdinalIgnoreCase))
                        output = DevGPTStoreConfigParser.Serialize(data);
                    else
                        output = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(filePath, output);
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

        private enum EditorMode { Text, Form, DevGptText }
        private EditorMode _editorMode = EditorMode.Text;

        private void RadioEditorMode_Checked(object sender, RoutedEventArgs e)
        {
            if (RadioDevGPTEditor != null && (RadioDevGPTEditor.IsChecked ?? false))
                _editorMode = EditorMode.DevGptText;
            else if (RadioTextEditor != null && (RadioTextEditor.IsChecked ?? false))
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
                StoresDevGPTEditor.Visibility = Visibility.Collapsed;
                var dummy = new ContentControl { Content = null };
                StoresEditorContent.Content = dummy;
            }
            else if (_editorMode == EditorMode.DevGptText)
            {
                StoresJsonEditor.Visibility = Visibility.Collapsed;
                StoresDevGPTEditor.Visibility = Visibility.Visible;
                var dummy = new ContentControl { Content = null };
                StoresEditorContent.Content = dummy;
            }
            else if (_editorMode == EditorMode.Form)
            {
                StoresJsonEditor.Visibility = Visibility.Collapsed;
                StoresDevGPTEditor.Visibility = Visibility.Collapsed;
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

        // Keep .devgpt & JSON text editors in sync. When switching, update content based on last parse.
        private void StoresJsonEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressEditorSync) return;
            if (_editorMode != EditorMode.Text) return;
            try
            {
                _suppressEditorSync = true;
                var stores = JsonSerializer.Deserialize<List<StoreConfig>>(StoresJsonEditor.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _parsedStores = stores ?? new List<StoreConfig>();
                _lastJsonText = StoresJsonEditor.Text;
                var devgpt = DevGPTStoreConfigParser.Serialize(_parsedStores);
                _lastDevGPTText = devgpt;
                StoresDevGPTEditor.Text = devgpt;
            }
            catch { }
            finally { _suppressEditorSync = false; }
        }
        private void StoresDevGPTEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressEditorSync) return;
            if (_editorMode != EditorMode.DevGptText) return;
            try
            {
                _suppressEditorSync = true;
                var stores = DevGPTStoreConfigParser.Parse(StoresDevGPTEditor.Text);
                _parsedStores = stores ?? new List<StoreConfig>();
                _lastDevGPTText = StoresDevGPTEditor.Text;
                var json = JsonSerializer.Serialize(_parsedStores, new JsonSerializerOptions { WriteIndented = true });
                _lastJsonText = json;
                StoresJsonEditor.Text = json;
            }
            catch { }
            finally { _suppressEditorSync = false; }
        }

        private string GetStoresEditorJsonText()
        {
            string format = SelectedStoreFormat;
            if (_editorMode == EditorMode.Text)
                return StoresJsonEditor.Text;
            else if (_editorMode == EditorMode.DevGptText)
                return StoresDevGPTEditor.Text;
            else
            {
                return format == "devgpt"
                    ? DevGPTStoreConfigParser.Serialize(_parsedStores)
                    : JsonSerializer.Serialize(_parsedStores, new JsonSerializerOptions { WriteIndented = true });
            }
        }

        // --- Nieuw chatvenster openen feature --- //
        private async void NewChatWindowButton_Click(object sender, RoutedEventArgs e)
        {
            EnsureLatestJson();

            const string LogFilePath = @"C:\\Projects\\devgpt\\log";
            var googleSettings = GoogleConfig.Load();
            var openAISettings = OpenAIConfig.Load();
            string openAIApiKey = openAISettings.ApiKey;

            var agentManager = new AgentManager(
                GetStoresEditorJsonText(),
                AgentsJsonEditor.Text,
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

        private void EnsureLatestJson()
        {
            try
            {
                string format = SelectedStoreFormat;
                if (_editorMode == EditorMode.Text)
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
                else if (_editorMode == EditorMode.DevGptText)
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

        // SETTINGS BUTTON HANDLER
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWin = new SettingsWindow();
            settingsWin.Owner = this;
            settingsWin.ShowDialog();
        }
    }
}
