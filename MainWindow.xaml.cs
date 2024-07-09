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
                string folderPath = FolderPathInput.Text;
                string embeddingsFile = EmbeddingsFileInput.Text;
                string historyFile = HistoryFileInput.Text;
                string query = QueryInput.Text;
                bool generateEmbeddings = GenerateEmbeddings.IsChecked == true;

                ProjectBuilder builder = new ProjectBuilder();
                var message = await builder.Ask(folderPath, embeddingsFile, historyFile, query, generateEmbeddings);
                MessageBox.Show(message);
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
                string folderPath = FolderPathInput.Text;
                string embeddingsFile = EmbeddingsFileInput.Text;
                string historyFile = HistoryFileInput.Text;
                string query = QueryInput.Text;
                bool generateEmbeddings = GenerateEmbeddings.IsChecked == true;

                ProjectBuilder builder = new ProjectBuilder();
                var message = await builder.Run(folderPath, embeddingsFile, historyFile, query, generateEmbeddings);
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