using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;

namespace NewWpfApp
{
    // Dummy EmbeddingFileStore for demonstration (replace by real implementation)
    public class EmbeddingFileStore
    {
        public string LastLoadedFile { get; private set; } = string.Empty;
        public bool SuccessLastRead { get; private set; } = false;
        public string ErrorMessage { get; private set; } = string.Empty;
        public List<string> LastFileNames { get; } = new();

        public bool OpenOrReadFile(string filePath)
        {
            LastLoadedFile = filePath;
            LastFileNames.Clear();
            SuccessLastRead = false;
            ErrorMessage = string.Empty;
            try
            {
                if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    string content = File.ReadAllText(filePath);
                    var doc = JsonNode.Parse(content);
                    // Optie 1: array van objecten met property "filename" of "file"
                    if (doc is JsonArray arr)
                    {
                        foreach (var item in arr)
                        {
                            if (item == null) continue;
                            string? fname = null;
                            if (item["filename"] != null) fname = item["filename"]!.ToString();
                            else if (item["file"] != null) fname = item["file"]!.ToString();
                            if (!string.IsNullOrEmpty(fname)) LastFileNames.Add(fname);
                        }
                    }
                    // Optie 2: dict met "files": []
                    else if (doc["files"] is JsonArray filesArr)
                    {
                        foreach (var val in filesArr)
                        {
                            if (val == null) continue;
                            if (val is JsonValue v && v.TryGetValue(out string s))
                                LastFileNames.Add(s);
                            else if (val["filename"] != null)
                                LastFileNames.Add(val["filename"]!.ToString());
                        }
                    }
                    // Optie 3: dict met "filenames": []
                    else if (doc["filenames"] is JsonArray fileNamesArr)
                    {
                        foreach (var val in fileNamesArr)
                        {
                            if (val is JsonValue v && v.TryGetValue(out string s)) LastFileNames.Add(s);
                        }
                    }
                    // fallback: geen errors
                    SuccessLastRead = true;
                    return true;
                }
                else if (filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".log", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".dat", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".lst", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var line in File.ReadLines(filePath))
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed)) LastFileNames.Add(trimmed);
                    }
                    SuccessLastRead = true;
                    return true;
                }
                else
                {
                    ErrorMessage = "Niet ondersteund bestandstype: " + Path.GetExtension(filePath);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }
    }

    public partial class MainWindow : Window
    {
        private EmbeddingFileStore embeddingsFileStore = new EmbeddingFileStore();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenAndLoadEmbeddingsbestand();
        }

        private void MenuOpenEmbeddingsbestand_Click(object sender, RoutedEventArgs e)
        {
            OpenAndLoadEmbeddingsbestand();
        }

        private void OpenAndLoadEmbeddingsbestand()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Alle bestanden (*.*)|*.*"; // allow all files
            dlg.Title = "Selecteer een embeddings bestand";

            if (dlg.ShowDialog() == true)
            {
                string filePath = dlg.FileName;
                ListFiles.Items.Clear();
                TxtStatus.Text = $"Bestand wordt ingelezen: {System.IO.Path.GetFileName(filePath)} ...";
                bool ok = embeddingsFileStore.OpenOrReadFile(filePath);

                if (ok && embeddingsFileStore.SuccessLastRead)
                {
                    TxtStatus.Text = $"{embeddingsFileStore.LastFileNames.Count} items uit: {System.IO.Path.GetFileName(filePath)}";
                    foreach (var name in embeddingsFileStore.LastFileNames)
                        ListFiles.Items.Add(name);
                }
                else
                {
                    TxtStatus.Text = $"Fout bij openen: {embeddingsFileStore.ErrorMessage}";
                }
            }
        }
    }
}
