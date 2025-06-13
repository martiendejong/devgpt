using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DevGPT;

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
