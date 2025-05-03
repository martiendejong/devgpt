namespace Store.OpnieuwOpnieuw
{
    public class MemoryStore : BaseStore
    {
        public Dictionary<string, string> Data { get; set; }

        public MemoryStore(IEmbeddingProvider embeddingProvider) : base(embeddingProvider)
        {
            Data = new Dictionary<string, string>();
        }

        public override void StoreData(string key, string value)
        {
            Data[key] = value;
        }

        public override void RemoveData(string key)
        {
            Data.Remove(key);
        }
    }
}
