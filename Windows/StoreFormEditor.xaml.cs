using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32; // Voor OpenFileDialog
using System.Windows.Forms;
using TextBox = System.Windows.Controls.TextBox; // Voor FolderBrowserDialog

namespace DevGPT
{
    public partial class StoreFormEditor : System.Windows.Controls.UserControl
    {
        public StoreConfig Store { get; set; }
        public StoreFormEditor(StoreConfig store)
        {
            InitializeComponent();
            Store = store;
            DataContext = Store;
        }

        private void BrowsePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            // We blijven FolderBrowserDialog gebruiken omdat WPF zelf geen folder picker heeft
#if NETCOREAPP || NET5_0_OR_GREATER
            dialog.InitialDirectory = Store.Path?.Replace("\\\\", "\\");
#else
            dialog.SelectedPath = Store.Path?.Replace("\\\\", "\\");
#endif
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // Escape backslashes voor JSON: C:\Test -> C:\\Test
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
    }
}
