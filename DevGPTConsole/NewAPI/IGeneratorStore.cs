using OpenAI_API.Chat;

namespace DevGPT.NewAPI
{
    public interface IGeneratorStore
    {
        Task<string> Generator_Question(string query, IEnumerable<ChatMessage>? messages);
        Task<string> Generator_UpdateStore(string query, IEnumerable<ChatMessage>? messages);
    }
}