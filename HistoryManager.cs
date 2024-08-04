using Newtonsoft.Json;
using OpenAI_API.Chat;
using System.IO;

public static class HistoryManager
{
    public static async Task<List<ChatMessage>> GetHistory(string historyFile)
    {
        var history = new List<ChatMessage>();
        if (historyFile != null && File.Exists(historyFile))
        {
            try
            {
                var entries = JsonConvert.DeserializeObject<List<HistoryEntry>>(await File.ReadAllTextAsync(historyFile));
                history = entries.Select(e => new ChatMessage(ChatMessageRole.FromString(e.Role), e.Content)).ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Cannot read history file {historyFile}");
                history = new List<ChatMessage>();
            }
        }

        return history;
    }
}