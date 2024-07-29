using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ConsoleApp1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            FolderPathInput.Text = @"C:\projects\ParisProof";
            FolderPathInput.Foreground = Brushes.Black;
            EmbeddingsFileInput.Text = @"c:\projects\embeddings.json";
            EmbeddingsFileInput.Foreground = Brushes.Black;
            HistoryFileInput.Text = @"c:\projects\history.json";
            HistoryFileInput.Foreground = Brushes.Black;
        }

        private void SetFormEnabled(bool enabled)
        {
            this.Background = enabled ? Brushes.White : Brushes.Gray;
            RunButton.IsEnabled = enabled;
            AskButton.IsEnabled = enabled;
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

                ProjectBuilder builder = new ProjectBuilder(config);
                var message = await builder.Ask();
                MessageBox.Show(message);
                AnswerOutput.Text += message + "\n";
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
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

                ProjectBuilder builder = new ProjectBuilder(config);
                var message = await builder.Run();
                MessageBox.Show(message);
            }
            catch(Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
            SetFormEnabled(true);
        }

        private void ClearPlaceholderText(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
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
                else if (textBox == QueryInput) textBox.Text = "Enter your query here...";
            }
        }
    }
}
