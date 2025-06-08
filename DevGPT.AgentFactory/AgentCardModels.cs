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
        private string _name;
        private string _description;
        private string _prompt;
        private ObservableCollection<StoreEntry> _stores = new ObservableCollection<StoreEntry>();
        private ObservableCollection<string> _functions = new ObservableCollection<string>();
        private ObservableCollection<string> _callsAgents = new ObservableCollection<string>();
        private ObservableCollection<string> _callsFlows = new ObservableCollection<string>();
        private bool _explicitModify;
        
        // Properties with INotifyPropertyChanged
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } }
        }
        public string Description
        {
            get => _description;
            set { if (_description != value) { _description = value; OnPropertyChanged(nameof(Description)); } }
        }
        public string Prompt
        {
            get => _prompt;
            set { if (_prompt != value) { _prompt = value; OnPropertyChanged(nameof(Prompt)); } }
        }
        public ObservableCollection<StoreEntry> Stores
        {
            get => _stores;
            set { if (_stores != value) { _stores = value; OnPropertyChanged(nameof(Stores)); } }
        }
        public ObservableCollection<string> Functions
        {
            get => _functions;
            set { if (_functions != value) { _functions = value; OnPropertyChanged(nameof(Functions)); } }
        }
        public ObservableCollection<string> CallsAgents
        {
            get => _callsAgents;
            set { if (_callsAgents != value) { _callsAgents = value; OnPropertyChanged(nameof(CallsAgents)); } }
        }
        public ObservableCollection<string> CallsFlows
        {
            get => _callsFlows;
            set { if (_callsFlows != value) { _callsFlows = value; OnPropertyChanged(nameof(CallsFlows)); } }
        }
        public bool ExplicitModify
        {
            get => _explicitModify;
            set { if (_explicitModify != value) { _explicitModify = value; OnPropertyChanged(nameof(ExplicitModify)); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

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
