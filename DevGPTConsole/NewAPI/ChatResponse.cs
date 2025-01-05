using Newtonsoft.Json;


namespace DevGPT.NewAPI
{
    public class SuccessResponse : ChatResponse<SuccessResponse>
    {
        public bool Success { get; set; }

        public SuccessResponse() : this(false) { }
        public SuccessResponse(bool value) { Success = value; }

        override public SuccessResponse _example { get => new SuccessResponse(false); }
        override public string _signature { get => @"{Success: bool}"; }
    }

    public abstract class ChatResponse<T> where T : ChatResponse<T>, new()
    {
        public static T Deserialize(string content) => JsonConvert.DeserializeObject<T>(content);
        public string Serialize() => JsonConvert.SerializeObject(this);
        public static T Example => new T()._example;
        [JsonIgnore]
        public abstract T _example { get; }
        public static string Signature => new T()._signature;
        [JsonIgnore]
        public abstract string _signature { get; }

        protected ChatResponse() { }
    }
}