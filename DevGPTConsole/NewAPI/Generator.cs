using OpenAI_API.Chat;
using OpenAI_API;
using OpenAI_API.Completions;

namespace DevGPT.NewAPI
{
    public class Generator
    {
        protected OpenAIAPI OpenAI { get; set; }
        public string SystemPrompt { get; set; } = "";
        public Generator(OpenAIAPI openai)
        {
            OpenAI = openai;
        }

        public string GetFormatInstruction<ResponseType>() where ResponseType : ChatResponse<ResponseType>, new()
            => $"YOUR OUTPUT WILL ALWAYS BE ONLY A JSON RESPONSE IN THIS FORMAT AND NOTHING ELSE: {ChatResponse<ResponseType>.Signature} EXAMPLE: {ChatResponse<ResponseType>.Example.Serialize()}";

        public async Task<ResponseType> GenerateObject<ResponseType>(IList<ChatMessage> messages) where ResponseType : ChatResponse<ResponseType>, new()
        {
            var formattingInstruction = GetFormatInstruction<ResponseType>();
            var systemInstruction = $"{SystemPrompt}\n{formattingInstruction}";
            messages = messages.Prepend(new ChatMessage(ChatMessageRole.System, systemInstruction)).ToList();
            
            var content = await Generate(messages, ChatRequest.ResponseFormats.JsonObject);
            var json = ChatResponse<ResponseType>.Deserialize(content);
            return json;
        }

        public async Task<string> Generate(IList<ChatMessage> messages, string responseFormat = ChatRequest.ResponseFormats.Text)
        {
            var response = await OpenAI.Chat.CreateChatCompletionAsync(new ChatRequest
            {
                Messages = messages,
                Model = "gpt-4o",
                ResponseFormat = responseFormat,
            });
            return response.Choices[0].Message.TextContent;
        }

        public async Task<string> Generate(string message, string responseFormat = ChatRequest.ResponseFormats.Text) 
            => await Generate(new ChatMessage[] { new ChatMessage(ChatMessageRole.User, message) }, responseFormat);
    }
}