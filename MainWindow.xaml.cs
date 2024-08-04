using System.Windows;
using System.Windows.Controls;
using Brushes=System.Windows.Media.Brushes;
using TextBox = System.Windows.Controls.TextBox;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using DevGPT;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace DevGPT
{
    public partial class MainWindow : Window
    {
        private List<AppBuilderConfig> _configurations = new List<AppBuilderConfig>();
        private const string ConfigFilePath = "configurations.json";

        public MainWindow()
        {
            InitializeComponent();
            FolderPathInput.Text = @"C:\projects\ParisProof";
            FolderPathInput.Foreground = Brushes.Black;
            EmbeddingsFileInput.Text = @"c:\projects\embeddings.json";
            EmbeddingsFileInput.Foreground = Brushes.Black;
            HistoryFileInput.Text = @"c:\projects\history.json";
            HistoryFileInput.Foreground = Brushes.Black;
            LoadConfigurations();
        }

        private void SetFormEnabled(bool enabled)
        {
            this.Background = enabled ? Brushes.White : Brushes.Gray;
            RunButton.IsEnabled = enabled;
            AskButton.IsEnabled = enabled;
            SaveButton.IsEnabled = enabled;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var h = new FolderBrowserHelper();
            var t = h.Help();
            if (t == null) return;
            EmbeddingsFileInput.Text = t;
            EmbeddingsFileInput.Foreground = Brushes.Black;
        }

        private void BrowseEmbeddingsFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                EmbeddingsFileInput.Text = dialog.FileName;
                EmbeddingsFileInput.Foreground = Brushes.Black;
            }
        }

        private void BrowseHistoryFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                HistoryFileInput.Text = dialog.FileName;
                HistoryFileInput.Foreground = Brushes.Black;
            }
        }

        public void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var config = new AppBuilderConfig
            {
                ProjectName = ProjectNameInput.Text,
                FolderPath = FolderPathInput.Text,
                EmbeddingsFile = EmbeddingsFileInput.Text,
                HistoryFile = HistoryFileInput.Text,
                GenerateEmbeddings = GenerateEmbeddings.IsChecked == true,
                UseHistory = GenerateHistory.IsChecked == true,
                Query = QueryInput.Text
            };

            var existingConfigIndex = _configurations.FindIndex(c => c.ProjectName == config.ProjectName);
            if (existingConfigIndex != -1)
            {
                _configurations[existingConfigIndex] = config;
            }
            else
            {
                _configurations.Add(config);
            }

            SaveConfigurations();
            LoadConfigurationsIntoDropdown();
        }

        private void LoadConfigurations()
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                _configurations = JsonConvert.DeserializeObject<List<AppBuilderConfig>>(json) ?? new List<AppBuilderConfig>();
                LoadConfigurationsIntoDropdown();
            }
        }

        public void SaveConfigurations()
        {
            var json = JsonConvert.SerializeObject(_configurations, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }

        private void LoadConfigurationsIntoDropdown()
        {
            ConfigDropdown.Items.Clear();
            foreach (var config in _configurations)
            {
                ConfigDropdown.Items.Add(config.ProjectName);
            }
        }

        private void ConfigDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedConfig = _configurations[ConfigDropdown.SelectedIndex];
            ProjectNameInput.Text = selectedConfig.ProjectName;
            FolderPathInput.Text = selectedConfig.FolderPath;
            EmbeddingsFileInput.Text = selectedConfig.EmbeddingsFile;
            HistoryFileInput.Text = selectedConfig.HistoryFile;
            GenerateEmbeddings.IsChecked = selectedConfig.GenerateEmbeddings;
            GenerateHistory.IsChecked = selectedConfig.UseHistory;
            QueryInput.Text = selectedConfig.Query;
        }

        private async void AskButton_Click(object sender, RoutedEventArgs e)
        {
            SetFormEnabled(false);
            try
            {
                var config = new AppBuilderConfig
                {
                    FolderPath = FolderPathInput.Text,
                    EmbeddingsFile = EmbeddingsFileInput.Text,
                    HistoryFile = HistoryFileInput.Text,
                    Query = QueryInput.Text,
                    GenerateEmbeddings = GenerateEmbeddings.IsChecked == true,
                    UseHistory = GenerateHistory.IsChecked == true
                };

                ProjectUpdater builder = new ProjectUpdater(config);
                var message = await builder.AnswerQuestion();
                System.Windows.Forms.MessageBox.Show(message);
                AnswerOutput.Text += message + "\n";
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("An error occurred: " + ex.Message);
            }
            SetFormEnabled(true);
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            SetFormEnabled(false);
            try
            {
                var config = new AppBuilderConfig
                {
                    FolderPath = FolderPathInput.Text,
                    EmbeddingsFile = EmbeddingsFileInput.Text,
                    HistoryFile = HistoryFileInput.Text,
                    Query = QueryInput.Text,
                    GenerateEmbeddings = GenerateEmbeddings.IsChecked == true,
                    UseHistory = GenerateHistory.IsChecked == true
                };

                ProjectUpdater builder = new ProjectUpdater(config);
                var message = await builder.UpdateCode();
                System.Windows.Forms.MessageBox.Show(message);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("An error occurred: " + ex.Message);
            }
            SetFormEnabled(true);
        }

        private void ClearPlaceholderText(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBox textBox = (TextBox)sender;
            if (textBox.Foreground == Brushes.Gray)
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }

        private void RestorePlaceholderText(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Foreground = Brushes.Gray;
                if (textBox == FolderPathInput) textBox.Text = "Project Folder Path";
                else if (textBox == EmbeddingsFileInput) textBox.Text = "Embeddings File";
                else if (textBox == HistoryFileInput) textBox.Text = "History File (optional)";
                else if (textBox == ProjectNameInput) textBox.Text = "Project Name";
                else if (textBox == QueryInput) textBox.Text = "Enter your query here...";
            }
        }

        private void PromptsButton_Click(object sender, RoutedEventArgs e)
        {
            PromptsWindow promptsWindow = new PromptsWindow(_configurations);
            promptsWindow.Show();
        }
    }
}