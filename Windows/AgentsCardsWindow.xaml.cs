using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Collections.Generic;
namespace DevGPT;
    public partial class AgentsCardsWindow : Window
    {
        private AgentsCardsBindingModel viewModel;
        public List<string> ResultAgents = null;

        public AgentsCardsWindow(AgentsCardsBindingModel model)
        {
            InitializeComponent();
            viewModel = model;
            DataContext = viewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        // Nieuw: knop event, voegt lege agent toe via bindingmodel
        private void AddNewAgentButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AddNewAgentCard();
        }

        // Nieuw: Handler voor toggelen van expand/collapse van een AgentCardModel
        private void ToggleAgentExpandButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            // De DataContext van de knop is het AgentCardModel
            if (btn?.DataContext is AgentCardModel card)
            {
                card.IsExpanded = !card.IsExpanded;
            }
        }

        // (Optioneel: verwijder uit xaml dubbele '+ Nieuwe agent' indien nodig)
        // Event om een agentcard te verwijderen
        private void RemoveAgentCardButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var card = btn?.CommandParameter as AgentCardModel;
            if (card != null)
            {
                viewModel.Cards.Remove(card);
            }
        }

        private void AddStoreButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var card = btn?.CommandParameter as AgentCardModel;
            if (card != null && !string.IsNullOrWhiteSpace(card.NewStoreToAdd))
            {
                if (!card.Stores.Any(s => s.Name == card.NewStoreToAdd))
                {
                    card.Stores.Add(new StoreRef
                    {
                        Name = card.NewStoreToAdd,
                        Write = card.NewStoreWritable
                    });
                }
                card.NewStoreToAdd = null;
                card.NewStoreWritable = false;
            }
        }

        private void RemoveStoreButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var store = btn?.CommandParameter as StoreRef;
            foreach (var card in viewModel.Cards)
            {
                if (card.Stores.Contains(store))
                {
                    card.Stores.Remove(store);
                    break;
                }
            }
        }

        private void AddFunctionButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var card = btn?.CommandParameter as AgentCardModel;
            if (card != null && !string.IsNullOrWhiteSpace(card.NewFunctionToAdd))
            {
                if (!card.Functions.Contains(card.NewFunctionToAdd))
                {
                    card.Functions.Add(card.NewFunctionToAdd);
                }
                card.NewFunctionToAdd = null;
            }
        }

        private void RemoveFunctionButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var func = btn?.CommandParameter as string;
            foreach (var card in viewModel.Cards)
            {
                if (card.Functions.Contains(func))
                {
                    card.Functions.Remove(func);
                    break;
                }
            }
        }

        private void AddAgentButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var card = btn?.CommandParameter as AgentCardModel;
            if (card != null && !string.IsNullOrWhiteSpace(card.NewAgentToAdd))
            {
                if (!card.CallsAgents.Contains(card.NewAgentToAdd))
                {
                    card.CallsAgents.Add(card.NewAgentToAdd);
                }
                card.NewAgentToAdd = null;
            }
        }

        private void RemoveAgentButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var agent = btn?.CommandParameter as string;
            foreach (var card in viewModel.Cards)
            {
                if (card.CallsAgents.Contains(agent))
                {
                    card.CallsAgents.Remove(agent);
                    break;
                }
            }
        }

        private void AddFlowButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var card = btn?.CommandParameter as AgentCardModel;
            if (card != null && !string.IsNullOrWhiteSpace(card.NewFlowToAdd))
            {
                if (!card.CallsFlows.Contains(card.NewFlowToAdd))
                {
                    card.CallsFlows.Add(card.NewFlowToAdd);
                }
                card.NewFlowToAdd = null;
            }
        }

        private void RemoveFlowButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var flow = btn?.CommandParameter as string;
            foreach (var card in viewModel.Cards)
            {
                if (card.CallsFlows.Contains(flow))
                {
                    card.CallsFlows.Remove(flow);
                    break;
                }
            }
        }

        private void OpslaanButton_Click(object sender, RoutedEventArgs e)
        {
            // Gather results and close dialog with OK
            ResultAgents = viewModel.Cards.Select(card => card.Name).ToList();
            DialogResult = true;
        }

        private void AnnuleerButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Niets op dit moment
        }

        private void ShowError(string message)
        {
            System.Windows.MessageBox.Show(this, message, "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public class AgentCardModel : INotifyPropertyChanged
    {
        private string name;
        private string description;
        private string prompt;
        private ObservableCollection<StoreRef> stores = new ObservableCollection<StoreRef>();
        private ObservableCollection<string> functions = new ObservableCollection<string>();
        private ObservableCollection<string> callsAgents = new ObservableCollection<string>();
        private ObservableCollection<string> callsFlows = new ObservableCollection<string>();
        private bool explicitModify;
        private string newStoreToAdd;
        private bool newStoreWritable = false;
        private string newFunctionToAdd;
        private string newAgentToAdd;
        private string newFlowToAdd;
        private bool isExpanded = false;

        public string Name { get => name; set { name = value; NotifyPropertyChanged(nameof(Name)); } }
        public string Description { get => description; set { description = value; NotifyPropertyChanged(nameof(Description)); } }
        public string Prompt { get => prompt; set { prompt = value; NotifyPropertyChanged(nameof(Prompt)); } }
        public ObservableCollection<StoreRef> Stores { get => stores; set { stores = value; NotifyPropertyChanged(nameof(Stores)); } }
        public ObservableCollection<string> Functions { get => functions; set { functions = value; NotifyPropertyChanged(nameof(Functions)); } }
        public ObservableCollection<string> CallsAgents { get => callsAgents; set { callsAgents = value; NotifyPropertyChanged(nameof(CallsAgents)); } }
        public ObservableCollection<string> CallsFlows { get => callsFlows; set { callsFlows = value; NotifyPropertyChanged(nameof(CallsFlows)); } }
        public bool ExplicitModify { get => explicitModify; set { explicitModify = value; NotifyPropertyChanged(nameof(ExplicitModify)); } }
        public string NewStoreToAdd { get => newStoreToAdd; set { newStoreToAdd = value; NotifyPropertyChanged(nameof(NewStoreToAdd)); } }
        public bool NewStoreWritable { get => newStoreWritable; set { newStoreWritable = value; NotifyPropertyChanged(nameof(NewStoreWritable)); } }
        public string NewFunctionToAdd { get => newFunctionToAdd; set { newFunctionToAdd = value; NotifyPropertyChanged(nameof(NewFunctionToAdd)); } }
        public string NewAgentToAdd { get => newAgentToAdd; set { newAgentToAdd = value; NotifyPropertyChanged(nameof(NewAgentToAdd)); } }
        public string NewFlowToAdd { get => newFlowToAdd; set { newFlowToAdd = value; NotifyPropertyChanged(nameof(NewFlowToAdd)); } }
        public bool IsExpanded { get => isExpanded; set { isExpanded = value; NotifyPropertyChanged(nameof(IsExpanded)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class AgentsCardsBindingModel : INotifyPropertyChanged
    {
        private ObservableCollection<AgentCardModel> cards;
        private List<string> allStores;
        private List<string> allFunctions;
        private List<string> allAgents;
        private List<string> allFlows;
        public ObservableCollection<AgentCardModel> Cards { get => cards; set { cards = value; NotifyPropertyChanged(nameof(Cards)); } }
        public List<string> AllStores { get => allStores; set { allStores = value; NotifyPropertyChanged(nameof(AllStores)); } }
        public List<string> AllFunctions { get => allFunctions; set { allFunctions = value; NotifyPropertyChanged(nameof(AllFunctions)); } }
        public List<string> AllAgents { get => allAgents; set { allAgents = value; NotifyPropertyChanged(nameof(AllAgents)); } }
        public List<string> AllFlows { get => allFlows; set { allFlows = value; NotifyPropertyChanged(nameof(AllFlows)); } }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // Nieuw: voegt een lege agentcard toe
        public void AddNewAgentCard()
        {
            Cards.Add(new AgentCardModel());
        }
    }
