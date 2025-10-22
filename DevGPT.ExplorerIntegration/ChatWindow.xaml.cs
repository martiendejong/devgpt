using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DevGPT.ExplorerIntegration
{
    public partial class ChatWindow : Window
    {
        private readonly DevGPTAgent _agent;
        private readonly string _folder;
        private readonly IDocumentStore _store;

        public ChatWindow(DevGPTAgent agent, string folder, IDocumentStore store)
        {
            InitializeComponent();
            _agent = agent;
            _folder = folder;
            _store = store;
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            var text = InputBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;
            AddMessage("You", text);
            InputBox.Text = string.Empty;
            try
            {
                // Use UpdateStore so modifications are applied when needed
                var response = await _agent.Generator.UpdateStore(text, CancellationToken.None, null, true, true, _agent.Tools, null);
                AddMessage("Assistant", response);
            }
            catch (System.Exception ex)
            {
                AddMessage("Error", ex.Message);
            }
        }

        private void AddMessage(string author, string text)
        {
            var border = new Border { BorderThickness = new Thickness(1), Margin = new Thickness(0, 4, 0, 4), Padding = new Thickness(6), CornerRadius = new CornerRadius(4), BorderBrush = SystemColors.ControlDarkBrush };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = author, FontWeight = FontWeights.Bold });
            sp.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap });
            border.Child = sp;
            MessagesPanel.Children.Add(border);
        }

        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EmbedDialog(_folder);
            dialog.Title = "Re-embed Files";
            if (dialog.ShowDialog() == true)
            {
                var button = (Button)sender;
                button.IsEnabled = false;
                button.Content = "Re-embedding...";
                try
                {
                    await EmbeddingService.GenerateEmbeddingsAsync(
                        _folder,
                        dialog.FileFilters,
                        dialog.OpenAIKey,
                        dialog.SaveKeyToRegistry);

                    AddMessage("System", "Embeddings updated successfully. The new files are now available for querying.");

                    // Reload the store
                    await _store.UpdateEmbeddings();
                }
                catch (System.Exception ex)
                {
                    AddMessage("Error", $"Failed to update embeddings: {ex.Message}");
                }
                finally
                {
                    button.IsEnabled = true;
                    button.Content = "⚙ Settings";
                }
            }
        }
    }
}

