using Newtonsoft.Json;


namespace DevGPT.NewAPI
{
    public abstract class ChatResponse<T> where T : ChatResponse<T>, new()
    {
        public static T Deserialize(string content) => JsonConvert.DeserializeObject<T>(content);
        public string Serialize() => JsonConvert.SerializeObject(this);
        public static T Example => new T()._example;
        [JsonIgnore]
        public abstract T _example { get; }
        protected ChatResponse() { }
    }
}