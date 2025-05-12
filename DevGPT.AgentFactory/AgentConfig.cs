public class AgentConfig
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Prompt { get; set; }
    public List<StoreRef> Stores { get; set; }
    public List<string> Functions { get; set; }
    public List<string> CallsAgents { get; set; }
    public bool ExplicitModify { get; set; } = false;
}
