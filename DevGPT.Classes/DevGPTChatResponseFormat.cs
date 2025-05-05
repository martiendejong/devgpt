namespace Store.OpnieuwOpnieuw.AIClient
{
    public class DevGPTChatResponseFormat
    {
        public string Format;
        protected DevGPTChatResponseFormat(string format) => Format = format;
        public static readonly DevGPTChatResponseFormat Text = new DevGPTChatResponseFormat("text");
        public static readonly DevGPTChatResponseFormat Json = new DevGPTChatResponseFormat("json");
    }
}