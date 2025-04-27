public interface IToolsContext
{
    List<Tool> Tools { get; set; }
    void Add(ToolInfo info);
}
