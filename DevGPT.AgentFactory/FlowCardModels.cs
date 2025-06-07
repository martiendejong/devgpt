using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DevGPT.AgentFactory
{
    /// <summary>
    /// Model voor het binden van FlowConfig aan WPF UI met ObservableCollection en INotifyPropertyChanged.
    /// </summary>
    public class FlowCardModel : INotifyPropertyChanged
    {
        private string _name;
        private string _description;
        private ObservableCollection<string> _callsAgents;
        private string _newAgentToAdd;

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

        public ObservableCollection<string> CallsAgents
        {
            get => _callsAgents;
            set { if (_callsAgents != value) { _callsAgents = value; OnPropertyChanged(nameof(CallsAgents)); } }
        }

        public string NewAgentToAdd
        {
            get => _newAgentToAdd;
            set { if (_newAgentToAdd != value) { _newAgentToAdd = value; OnPropertyChanged(nameof(NewAgentToAdd)); } }
        }

        public FlowCardModel()
        {
            _callsAgents = new ObservableCollection<string>();
        }

        public FlowCardModel(FlowConfig flow)
        {
            _name = flow.Name;
            _description = flow.Description;
            _callsAgents = new ObservableCollection<string>(flow.CallsAgents != null ? flow.CallsAgents : new List<string>());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }

        /// <summary>
        /// Zet deze FlowCardModel terug om naar FlowConfig
        /// </summary>
        public FlowConfig ToFlowConfig()
        {
            return new FlowConfig
            {
                Name = this.Name,
                Description = this.Description,
                CallsAgents = this.CallsAgents != null ? this.CallsAgents.ToList() : new List<string>()
            };
        }
    }

    /// <summary>
    /// Root bindingmodel voor meerdere flowkaart-modellen plus keuzedata
    /// </summary>
    public class FlowCardsBindingModel : INotifyPropertyChanged
    {
        public ObservableCollection<FlowCardModel> Cards { get; set; } = new ObservableCollection<FlowCardModel>();
        public List<string> AllAgents { get; set; } = new List<string>();
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string property) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property)); }
    }
}