using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using DevGPT;

namespace DevGPT
{
    public class AgentConfigViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private AgentConfig _agent;

        public AgentConfigViewModel(AgentConfig agent)
        {
            _agent = agent;
            // Provide string conversion helpers for comma separated UI editing
        }

        public string Name
        {
            get => _agent.Name;
            set { _agent.Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
        }

        public string Description
        {
            get => _agent.Description;
            set { _agent.Description = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description))); }
        }
        public string Prompt
        {
            get => _agent.Prompt;
            set { _agent.Prompt = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Prompt))); }
        }

        public List<StoreRef> Stores
        {
            get => _agent.Stores ??= new List<StoreRef>();
            set { _agent.Stores = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Stores))); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StoresString))); }
        }

        // name:write (write=1/0)
        public string StoresString
        {
            get => Stores == null ? "" : string.Join(", ", Stores.Select(s => $"{s.Name}:{(s.Write ? 1 : 0)}"));
            set
            {
                Stores = value.Split(',').Select(s =>
                {
                    var parts = s.Trim().Split(':');
                    if (parts.Length == 2)
                        return new StoreRef { Name = parts[0], Write = parts[1].Trim() == "1" || parts[1].Trim().ToLowerInvariant() == "true" };
                    return new StoreRef { Name = parts[0] };
                }).Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StoresString)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Stores)));
            }
        }

        public List<string> Functions
        {
            get => _agent.Functions ??= new List<string>();
            set { _agent.Functions = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Functions))); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FunctionsString))); }
        }
        public string FunctionsString
        {
            get => Functions == null ? "" : string.Join(", ", Functions);
            set
            {
                Functions = value.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FunctionsString)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Functions)));
            }
        }

        public List<string> CallsAgents
        {
            get => _agent.CallsAgents ??= new List<string>();
            set { _agent.CallsAgents = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CallsAgents))); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CallsAgentsString))); }
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

        public bool ExplicitModify
        {
            get => _agent.ExplicitModify;
            set { _agent.ExplicitModify = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExplicitModify))); }
        }
        public AgentConfig GetAgentConfig() => _agent;
    }

    public partial class AgentFormEditor : System.Windows.Controls.UserControl
    {
        public AgentConfigViewModel AgentVM { get; set; }
        public AgentFormEditor(AgentConfig agent)
        {
            InitializeComponent();
            AgentVM = new AgentConfigViewModel(agent);
            DataContext = AgentVM;
        }
    }
}
