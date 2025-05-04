using DevGPT.NewAPI;

namespace Store.OpnieuwOpnieuw
{
    public class EmbeddingInfo
    {
        public string Key { get; set; }
        public Embedding Data { get; set; }
        public string Checksum { get; set; }
        public EmbeddingInfo(string key, Embedding data, string checksum)
        {
            Key = key;
            Data = data;
            Checksum = checksum;
        }
    }
}
