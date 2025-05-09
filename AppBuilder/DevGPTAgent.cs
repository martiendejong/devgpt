// See https://aka.ms/new-console-template for more information
using DevGPT.NewAPI;

public class DevGPTAgent
{
    public string Name { get; set; }
    public DocumentGenerator Generator { get; set; }
    public ToolsContextBase Tools { get; set; }

    public DevGPTAgent(string name, DocumentGenerator generator, ToolsContextBase tools)
    {
        Name = name;
        Generator = generator;
        Tools = tools;
    }
}
