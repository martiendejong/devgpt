// Carol: Robust error handling for invalid JSON or devgpt per TODO_implement_error_handling_for_invalid_formats.txt.
// - Shows clear UI feedback on parse issues
// - Prevents any unsynchronized save
// - Provides actionable suggestions
// - Save is only ever enabled with valid + synchronized fields

using System;
using System.Windows;
using System.Windows.Controls;
using TextBox = System.Windows.Controls.TextBox;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

namespace DevGPT
{
    public partial class StoreFormEditor : System.Windows.Controls.UserControl
    {
        public StoreConfig Store { get; set; }
        private string _jsonText;
        private string _devgptText;
        private bool _syncing = false;  // Prevent feedback loop
        private bool _jsonValid = true, _devgptValid = true;
        private string _lastJsonError = "", _lastDevgptError = "";

        // --- Undo/Redo/Versioning ---
        private Stack<(string Json, string DevGPT)> _undoStack = new();
        private Stack<(string Json, string DevGPT)> _redoStack = new();
        private List<(DateTime Timestamp, string Json, string DevGPT)> _history = new();

        public bool IsInputValid { get; private set; } = false;

        // --- File Versioning Paths ---
        private readonly string StoresJsonPath = "AppBuilder/stores.json";
        private readonly string StoresBackupDir = "AppBuilder/stores_backups";

        public StoreFormEditor(StoreConfig store)
        {
            InitializeComponent();
            Store = store;
            DataContext = Store;
            _jsonText = ToJson(store);
            _devgptText = ToDevgpt(store);
            JsonBox.Text = _jsonText;
            DevgptBox.Text = _devgptText;
            ClearErrorState();
            ClearHistory();
            AddHistory();
            ValidateAndSyncUI(announce: false);
        }

        // --- Format helpers (unchanged) ---
        private string ToJson(StoreConfig sc) => JsonSerializer.Serialize(new List<StoreConfig> { sc }, new JsonSerializerOptions { WriteIndented = true });
        private StoreConfig FromJson(string text, out string parseError, out string suggestion)
        {
            parseError = null; suggestion = null;
            try
            {
                if (string.IsNullOrWhiteSpace(text)) { parseError = "Het JSON veld is leeg."; return null; }
                var parsed = StoreConfigFormatHelper.AutoDetectAndParse(text);
                if (parsed == null || parsed.Count == 0) { parseError = "Geen geldige storeconfig in JSON."; return null; }
                return parsed[0];
            }
            catch (JsonException ex)
            {
                parseError = $"JSON fout: {GetCleanErrorMessage(ex.Message)}";
                suggestion = SuggestJsonCorrection(text, ex);
                return null;
            }
            catch (Exception ex)
            {
                parseError = ex.Message;
                return null;
            }
        }
        private string ToDevgpt(StoreConfig sc) => DevGPTStoreConfigParser.Serialize([sc]);
        private StoreConfig FromDevgpt(string text, out string parseError, out string suggestion)
        {
            parseError = null; suggestion = null;
            try
            {
                if (string.IsNullOrWhiteSpace(text)) { parseError = "Het devgpt veld is leeg."; return null; }
                var parsed = StoreConfigFormatHelper.AutoDetectAndParse(text);
                if (parsed == null || parsed.Count == 0) { parseError = "Geen geldige storeconfig in devgpt-formaat."; return null; }
                return parsed[0];
            }
            catch (Exception ex)
            {
                parseError = $"devgpt fout: {GetCleanErrorMessage(ex.Message)}";
                suggestion = SuggestDevgptCorrection(text, ex);
                return null;
            }
        }

        // --- Robust Error Handling and Feedback ---
        private void JsonBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncing) return;
            _syncing = true;
            _jsonText = JsonBox.Text;
            string parseError, suggestion;
            var sc = FromJson(_jsonText, out parseError, out suggestion);

            if (sc == null)
            {
                _jsonValid = false;
                _lastJsonError = parseError ?? "Onbekende fout";
                MarkTextBoxAsError(JsonBox);
                IsInputValid = false;
                ShowError($"JSON invoer ongeldig: {_lastJsonError}{(suggestion != null ? "\nSuggestie: " + suggestion : "")}");
                DisableSave();
            }
            else
            {
                _jsonValid = true;
                _lastJsonError = null;
                ClearTextBoxError(JsonBox);
                string candidateDevgpt = ToDevgpt(sc);
                if (DevgptBox.Text != candidateDevgpt) DevgptBox.Text = candidateDevgpt;
                _devgptText = candidateDevgpt;
                CopyStore(sc, Store);
                PushUndo(_jsonText, _devgptText);
                ValidateAndSyncUI();
            }
            _syncing = false;
        }

        private void DevgptBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncing) return;
            _syncing = true;
            _devgptText = DevgptBox.Text;
            string parseError, suggestion;
            var sc = FromDevgpt(_devgptText, out parseError, out suggestion);

            if (sc == null)
            {
                _devgptValid = false;
                _lastDevgptError = parseError ?? "Onbekende fout";
                MarkTextBoxAsError(DevgptBox);
                IsInputValid = false;
                ShowError($"devgpt invoer ongeldig: {_lastDevgptError}{(suggestion != null ? "\nSuggestie: " + suggestion : "")}");
                DisableSave();
            }
            else
            {
                _devgptValid = true;
                _lastDevgptError = null;
                ClearTextBoxError(DevgptBox);
                string candidateJson = ToJson(sc);
                if (JsonBox.Text != candidateJson) JsonBox.Text = candidateJson;
                _jsonText = candidateJson;
                CopyStore(sc, Store);
                PushUndo(_jsonText, _devgptText);
                ValidateAndSyncUI();
            }
            _syncing = false;
        }

        // Central method to check full validity and keep Save/feedback in sync
        private void ValidateAndSyncUI(bool announce = true)
        {
            bool valid = _jsonValid && _devgptValid;
            bool sync = (_jsonText == ToJson(Store)) && (_devgptText == ToDevgpt(Store));
            IsInputValid = valid && sync;
            SaveButton.IsEnabled = IsInputValid;

            if (!valid)
            {
                if (announce)
                    ShowError("Voer geldige JSON of devgpt-invoer in om op te slaan.");
            }
            else if (!sync)
            {
                if (announce)
                    ShowError("Wijzigingen zijn nog niet gesynchroniseerd – controleer beide invoervelden.");
                DisableSave();
            }
            else
            {
                if (announce)
                    ClearErrorState();
            }
        }

        private void DisableSave() => SaveButton.IsEnabled = false;

        // --- Undo/Redo Versioning (unchanged) ---
        public void Undo()
        {
            if (_undoStack.Count > 1) { var curr = (_jsonText, _devgptText); _redoStack.Push(curr); _undoStack.Pop(); var target = _undoStack.Peek(); _syncing = true; JsonBox.Text = target.Json; DevgptBox.Text = target.DevGPT; _jsonText = target.Json; _devgptText = target.DevGPT; _syncing = false; AddHistory(); ValidateAndSyncUI(); }
        }
        public void Redo()
        {
            if (_redoStack.Count > 0) { var next = _redoStack.Pop(); _undoStack.Push(next); _syncing = true; JsonBox.Text = next.Json; DevgptBox.Text = next.DevGPT; _jsonText = next.Json; _devgptText = next.DevGPT; _syncing = false; AddHistory(); ValidateAndSyncUI(); }
        }
        private void PushUndo(string json, string devgpt) { if (_undoStack.Count == 0 || _undoStack.Peek().Json != json || _undoStack.Peek().DevGPT != devgpt) { _undoStack.Push((json, devgpt)); _redoStack.Clear(); AddHistory(); } }
        private void ClearHistory() { _undoStack.Clear(); _redoStack.Clear(); _history.Clear(); }
        private void AddHistory() { _history.Add((DateTime.UtcNow, _jsonText, _devgptText)); if (_history.Count > 50) _history = _history.Skip(_history.Count - 50).ToList(); }
        public IReadOnlyList<(DateTime Timestamp, string Json, string DevGPT)> VersionHistory => _history.AsReadOnly();

        // --- UI Feedback & Helpers (unchanged except calls to ValidateAndSyncUI) ---
        private void ShowError(string text) => ErrorText.Text = text;
        private void ClearErrorState()
        {
            ErrorText.Text = string.Empty;
            ClearTextBoxError(JsonBox);
            ClearTextBoxError(DevgptBox);
        }
        private void MarkTextBoxAsError(TextBox box) { box.BorderThickness = new Thickness(2); box.BorderBrush = System.Windows.Media.Brushes.Red; }
        private void ClearTextBoxError(TextBox box) { box.BorderThickness = new Thickness(1); box.ClearValue(TextBox.BorderBrushProperty); }

        private void EditModeTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncing) return;
            _syncing = true;
            try
            {
                if (EditModeTab.SelectedIndex == 0)
                {
                    var sc = Store;
                    _jsonText = ToJson(sc);
                    if (JsonBox.Text != _jsonText) JsonBox.Text = _jsonText;
                }
                else
                {
                    var sc = Store;
                    _devgptText = ToDevgpt(sc);
                    if (DevgptBox.Text != _devgptText) DevgptBox.Text = _devgptText;
                }
            }
            catch { }
            _syncing = false;
            ValidateAndSyncUI();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ValidateAndSyncUI();
            if (!IsInputValid)
            {
                ShowError("Kan niet opslaan: invoer is ongeldig of niet gesynchroniseerd.");
                DisableSave();
                return;
            }

            try
            {
                // --- Detect and prevent concurrent file edits (simple last-write-win) ---
                var originalTimestamp = File.Exists(StoresJsonPath) ? File.GetLastWriteTimeUtc(StoresJsonPath) : DateTime.MinValue;

                // --- Load and update in-memory list ---
                var json = File.Exists(StoresJsonPath) ? File.ReadAllText(StoresJsonPath) : "[]";
                var list = JsonSerializer.Deserialize<List<StoreConfig>>(json) ?? new();
                var existing = list.FirstOrDefault(x => x.Name == Store.Name);
                if (existing != null)
                {
                    CopyStore(Store, existing);
                }
                else
                {
                    list.Add(Store);
                }
                var newJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });

                // --- Confirm file hasn't changed since loaded (concurrent modification check) ---
                var currentTimestamp = File.Exists(StoresJsonPath) ? File.GetLastWriteTimeUtc(StoresJsonPath) : DateTime.MinValue;
                if (currentTimestamp != originalTimestamp && originalTimestamp != DateTime.MinValue)
                {
                    ShowError("Let op: stores.json is gewijzigd door een andere bewerking. Herlaad eerst en probeer opnieuw.");
                    return;
                }

                // --- Robust Save ---
                Directory.CreateDirectory(StoresBackupDir);

                // 1. Backup current file (with timestamp)
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                if (File.Exists(StoresJsonPath))
                {
                    var backupPath = Path.Combine(StoresBackupDir, $"stores.json.{timestamp}.bak");
                    File.Copy(StoresJsonPath, backupPath, overwrite: true);
                }

                // 2. Write to temp file first
                var tempPath = StoresJsonPath + ".tmp";
                File.WriteAllText(tempPath, newJson);

                // 3. Atomically replace
                if (File.Exists(StoresJsonPath))
                {
                    File.Replace(tempPath, StoresJsonPath, null);
                }
                else
                {
                    File.Move(tempPath, StoresJsonPath);
                }

                // 4. Confirm save (optional: re-read content)
                var diskContents = File.ReadAllText(StoresJsonPath);
                if (diskContents != newJson)
                {
                    ShowError("Opslaan is mislukt: schijfinhoud wijkt af.");
                    return;
                }

                // --- Save was robust and versioned ---
                ShowError("Succesvol opgeslagen!");
                AddHistory();

                // Optionally remove oldest backups if >10 to keep history short
                var backups = Directory.GetFiles(StoresBackupDir)
                    .OrderByDescending(File.GetCreationTimeUtc)
                    .Skip(10)
                    .ToArray();
                foreach (var oldBak in backups)
                    File.Delete(oldBak);
            }
            catch (Exception ex)
            {
                ShowError($"Fout bij opslaan: {ex.Message}");
            }
        }

        // --- CopyStore and suggestions helpers (unchanged) ---
        private void CopyStore(StoreConfig src, StoreConfig dest)
        {
            dest.Name = src.Name;
            dest.Description = src.Description;
            dest.Path = src.Path;
            dest.FileFilters = src.FileFilters;
            dest.SubDirectory = src.SubDirectory;
            dest.ExcludePattern = src.ExcludePattern;
        }
        private void BrowsePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
#if NETCOREAPP || NET5_0_OR_GREATER
            dialog.InitialDirectory = Store.Path?.Replace("\\\\", "\\");
#else
            dialog.SelectedPath = Store.Path?.Replace("\\\\", "\\");
#endif
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string esc = dialog.SelectedPath.Replace("\\", "\\\\");
                Store.Path = esc;
                PathBox.Text = esc;
            }
        }
        private void PathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var box = sender as TextBox;
            if (box != null)
            {
                string esc = box.Text.Replace("\\", "\\\\");
                if (box.Text != esc)
                {
                    int sel = box.SelectionStart;
                    box.Text = esc;
                    box.SelectionStart = esc.Length;
                }
                Store.Path = esc;
            }
        }
        private string SuggestJsonCorrection(string text, JsonException ex)
        {
            var msg = ex.Message;
            if (msg.Contains("Unexpected end of JSON input") || msg.Contains("Incomplete"))
                return "Controleer ontbrekende accolades '}' of vierkante haken ']' aan het einde.";
            if (msg.Contains(":"))
                return "Er mist mogelijk een dubbele punt ':' na een propertynaam.";
            if (msg.Contains("trailing comma"))
                return "Verwijder komma's na het laatste element of property.";
            return null;
        }
        private string SuggestDevgptCorrection(string text, Exception ex)
        {
            if (!text.Contains(':'))
                return "Er mist mogelijk een dubbele punt (:) na een veldnaam.";
            if (!Regex.IsMatch(text, "^Name:", RegexOptions.Multiline))
                return "Begin met 'Name: <naam>' op de eerste regel.";
            return null;
        }
        private string GetCleanErrorMessage(string msg)
        {
            if (msg == null) return null;
            int idx = msg.IndexOf(". Path:");
            if (idx > 0) return msg.Substring(0, idx);
            return msg;
        }
    }
}
