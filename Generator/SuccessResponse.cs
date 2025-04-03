using System.Text.Json.Serialization;

namespace DevGPT.NewAPI
{
    public class SuccessResponse : ChatResponse<SuccessResponse>
    {
        public bool Success { get; set; }

        public SuccessResponse() : this(false) { }
        public SuccessResponse(bool value) { Success = value; }

        [JsonIgnore]
        override public SuccessResponse _example { get => new SuccessResponse(false); }

        [JsonIgnore]
        override public string _signature { get => @"{Success: bool}"; }
    }
}