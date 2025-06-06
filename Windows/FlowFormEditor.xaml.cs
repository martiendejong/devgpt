using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using DevGPT;

namespace DevGPT
{
    public class FlowConfigViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private FlowConfig _flow;

        public FlowConfigViewModel(FlowConfig flow)
        {
            _flow = flow;
            // Provide string conversion helpers for comma separated UI editing
        }

        public string Name
        {
            get => _flow.Name;
            set { _flow.Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
        }

        public string Description
        {
            get => _flow.Description;
            set { _flow.Description = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description))); }
        }

        public List<string> CallsAgents
        {
            get => _flow.CallsAgents ??= new List<string>();
            set { _flow.CallsAgents = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CallsAgents))); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CallsAgentsString))); }
        }
        public string CallsAgentsString
        {
            get => CallsAgents == null ? "" : string.Join(", ", CallsAgents);
            set
            {
                CallsAgents = value.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CallsAgentsString)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CallsAgents)));
            }
        }
        public FlowConfig GetFlowConfig() => _flow;
    }

    public partial class FlowFormEditor : System.Windows.Controls.UserControl
    {
        public FlowConfigViewModel FlowVM { get; set; }
        public FlowFormEditor(FlowConfig flow)
        {
            InitializeComponent();
            FlowVM = new FlowConfigViewModel(flow);
            DataContext = FlowVM;
        }
    }
}
