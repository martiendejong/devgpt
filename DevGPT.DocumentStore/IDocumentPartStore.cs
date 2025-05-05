namespace Store.OpnieuwOpnieuw.DocumentStore
{
    public interface IDocumentPartStore : IDictionary<string, IEnumerable<string>>
    {
        public Task Store(string name, IEnumerable<string> partKeys);
    }
}
