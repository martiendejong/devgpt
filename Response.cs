public class Response
{
    public string Message { get; set; }
    public List<Change> Changes { get; set; }
    public List<string> Deletions { get; set; }
}