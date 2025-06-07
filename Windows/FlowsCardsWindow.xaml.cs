using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls; // Toegevoegd voor Button etc.
using System.Windows.Input;

// Eventuele modelimports (bij FlowConfig, als hij elders zit):
// using DevGPT.AgentFactory; // Indien FlowConfig, FlowCardModel elders gedefinieerd zijn

namespace DevGPT
{
    public class FlowCardModel : INotifyPropertyChanged
    {
        public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } } }
        private string _name;
        public string Description { get => _description; set { if (_description != value) { _description = value; OnPropertyChanged(nameof(Description)); } } }
        private string _description;
        public ObservableCollection<string> CallsAgents { get; set; } = new ObservableCollection<string>();
        public string NewAgentToAdd { get => _newAgentToAdd; set { if (_newAgentToAdd != value) { _newAgentToAdd = value; OnPropertyChanged(nameof(NewAgentToAdd)); } } }
        private string _newAgentToAdd;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string n) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n)); }
    }
    public class FlowCardsBindingModel : INotifyPropertyChanged
    {
        public ObservableCollection<FlowCardModel> Cards { get; set; } = new ObservableCollection<FlowCardModel>();
        public List<string> AllAgents { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string n) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n)); }
    }
    public partial class FlowsCardsWindow : Window
    {
        public FlowCardsBindingModel Model { get; set; }
        public FlowsCardsWindow(FlowCardsBindingModel model)
        {
            InitializeComponent();
            DataContext = Model = model;
        }
        public List<FlowConfig> ResultFlows { get; set; } = null;
        private void OpslaanButton_Click(object sender, RoutedEventArgs e)
        {
            // Zet Cards om naar FlowConfig[]
            var list = Model.Cards.Select(card => new FlowConfig
            {
                Name = card.Name,
                Description = card.Description,
                CallsAgents = card.CallsAgents.ToList()
            }).ToList();
            ResultFlows = list;
            DialogResult = true;
            Close();
        }
        private void AnnuleerButton_Click(object sender, RoutedEventArgs e)
        {
            ResultFlows = null;
            DialogResult = false;
            Close();
        }
        private void AddAgentButton_Click(object sender, RoutedEventArgs e)
        {
            var b = (sender as System.Windows.Controls.Button);
            var card = b?.CommandParameter as FlowCardModel;
            if (card != null && !string.IsNullOrWhiteSpace(card.NewAgentToAdd))
            {
                if (!card.CallsAgents.Contains(card.NewAgentToAdd))
                {
                    card.CallsAgents.Add(card.NewAgentToAdd);
                }
                card.NewAgentToAdd = null;
            }
        }
    }
}