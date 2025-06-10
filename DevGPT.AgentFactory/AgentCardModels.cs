using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Collections.Specialized;

    /// <summary>
    /// Represents a store entry for agents; supports WPF data binding.
    /// </summary>
    public class StoreEntry : INotifyPropertyChanged
    {
        private string _storeName;
        private bool _writable;
        public string StoreName
        {
            get => _storeName;
            set { if (_storeName != value) { _storeName = value; OnPropertyChanged(nameof(StoreName)); } }
        }
        public bool Writable
        {
            get => _writable;
            set { if (_writable != value) { _writable = value; OnPropertyChanged(nameof(Writable)); } }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }

    /// <summary>
    /// Agent binding card model for WPF UI.
    /// </summary>
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

        // Scaffold: Add events/commands for adding/removing stores, functions, calls
        // TODO: Implement add/remove logic and helper methods, following FlowCardModel patterns
    }

    /// <summary>
    /// Collection binding model for multiple agent card models.
    /// </summary>
    public class AgentsCardsBindingModel : INotifyPropertyChanged
    {
        private ObservableCollection<AgentCardModel> _cards = new ObservableCollection<AgentCardModel>();
        private List<string> _allStores = new List<string>();
        private List<string> _allFunctions = new List<string>();
        private List<string> _allAgents = new List<string>();
        private List<string> _allFlows = new List<string>();
        private bool _isModified;

        public ObservableCollection<AgentCardModel> Cards
        {
            get => _cards;
            set { if (_cards != value) { _cards = value; OnPropertyChanged(nameof(Cards)); } }
        }
        public List<string> AllStores
        {
            get => _allStores;
            set { if (_allStores != value) { _allStores = value; OnPropertyChanged(nameof(AllStores)); } }
        }
        public List<string> AllFunctions
        {
            get => _allFunctions;
            set { if (_allFunctions != value) { _allFunctions = value; OnPropertyChanged(nameof(AllFunctions)); } }
        }
        public List<string> AllAgents
        {
            get => _allAgents;
            set { if (_allAgents != value) { _allAgents = value; OnPropertyChanged(nameof(AllAgents)); } }
        }
        public List<string> AllFlows
        {
            get => _allFlows;
            set { if (_allFlows != value) { _allFlows = value; OnPropertyChanged(nameof(AllFlows)); } }
        }
        public bool IsModified
        {
            get => _isModified;
            set { if (_isModified != value) { _isModified = value; OnPropertyChanged(nameof(IsModified)); } }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        public AgentsCardsBindingModel()
        {
            _cards.CollectionChanged += Cards_CollectionChanged;
        }

        private void Cards_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Attach event listeners for new agent cards, as in FlowCardsBindingModel
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is AgentCardModel model)
                    {
                        model.PropertyChanged += OnCardPropertyChanged;
                    }
                }
            }
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is AgentCardModel model)
                        model.PropertyChanged -= OnCardPropertyChanged;
                }
            }
            IsModified = true;
        }
        private void OnCardPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IsModified = true;
        }
        public void HookCardPropertyChangedHandlers()
        {
            foreach (var card in Cards)
                card.PropertyChanged += OnCardPropertyChanged;
        }
    }
