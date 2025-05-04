namespace Store.OpnieuwOpnieuw.DocumentStore
{
    public interface IDocumentPartStore : IDictionary<string, IEnumerable<string>>
    {
        public void Store(string name, IEnumerable<string> partKeys);
    }
}
