using System.Collections.Generic;
using System.Windows;

namespace DevGPT
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
                SystemInstructions1.Text = config.SystemInstructions1;
                SystemInstructions2.Text = config.SystemInstructions2;
            }
        }

        private void SavePromptsButton_Click(object sender, RoutedEventArgs e)
        {
            var config = _configurations[0]; // assume we have one config to keep things simple
            config.SystemInstructions1 = SystemInstructions1.Text;
            config.SystemInstructions2 = SystemInstructions2.Text;

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
