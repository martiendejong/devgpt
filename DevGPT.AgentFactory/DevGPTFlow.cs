public class DevGPTFlow
{
    public string Name { get; set; }
    public List<string> Agents { get; set; }
    public DevGPTFlow(string name, List<string> agents)
    {
        Agents = agents;
    }
}