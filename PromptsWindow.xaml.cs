using System.Collections.Generic;
using System.Windows;

namespace ConsoleApp1
{
    public partial class PromptsWindow : Window
    {
        private List<AppBuilderConfig> _configurations;
        public PromptsWindow(List<AppBuilderConfig> configurations)
        {
            InitializeComponent();
            _configurations = configurations;
            LoadPrompts();
        }

        private void LoadPrompts()
        {
            if (_configurations?.Count > 0)
            {
                var config = _configurations[0]; // assume we have one config to keep things simple
                Prompt1.Text = config.Query;
                Prompt2.Text = "Another Prompt Field"; // add more fields as needed
                Prompt3.Text = "More Prompt Content";
            }
        }

        private void SavePromptsButton_Click(object sender, RoutedEventArgs e)
        {
            var config = _configurations[0]; // assume we have one config to keep things simple
            config.Query = Prompt1.Text;
            // Save other prompts as needed

            // save back the configuration
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.SaveConfigurations();
            }

            this.Close();
        }
    }
}