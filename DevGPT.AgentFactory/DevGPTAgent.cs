public class DevGPTAgent
{
    public string Name { get; set; }
    public DocumentGenerator Generator { get; set; }
    public ToolsContextBase Tools { get; set; }
    public bool IsCoder = false;

    public DevGPTAgent(string name, DocumentGenerator generator, ToolsContextBase tools, bool isCoder = false)
    {
        Name = name;
        Generator = generator;
        Tools = tools;
        IsCoder = isCoder;
    }
}