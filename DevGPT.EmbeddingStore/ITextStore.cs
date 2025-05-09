namespace Store.OpnieuwOpnieuw
{
    public interface ITextStore
    {
        public Task<bool> Store(string key, string value);
        public Task<string> Get(string key);
        public string GetPath(string key);
        public Task<bool> Remove(string key);
    }
}
