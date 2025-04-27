using OpenAI.Chat;
using static System.Runtime.InteropServices.JavaScript.JSType;


public interface ILogger
{
    void Log(List<ChatMessage> messages, string responseContent);
}

public class Logger : ILogger
{
    private string LogFilePath;

    public Logger(string logFilePath) { LogFilePath = logFilePath; }

    public static string GetMessageType(ChatMessage m)
    {
        if (m is AssistantChatMessage) return "Assistant";
        if (m is UserChatMessage) return "User";
        if (m is SystemChatMessage) return "System";
        if (m is ToolChatMessage) return "Tool";
        return "Unknown";
    }

    public void Log(string message)
    {
        message = $"{DateTime.Now.ToString("yy-MM-dd hh:mm")}: " + message;
        if (File.Exists(LogFilePath))
        {
            File.AppendAllText(LogFilePath, "\n" + message);
        }
        else
        {
            File.WriteAllText(LogFilePath, message);
        }
    }

    public void Log(List<ChatMessage> messages, string responseContent)
    {
        var logEntry = new LogEntry();
        logEntry.Date = DateTime.Now.ToString("MM-dd-yy HH:mm");
        logEntry.Source = GetType().Name;
        logEntry.Messages = messages.Select(m => new LogMessage { Role = GetMessageType(m), Message = m.Content.FirstOrDefault()?.Text ?? "" }).ToList();

        var message = new LogMessage { Message = responseContent, Role = "Assistant" };
        logEntry.Messages.Add(message);

        var data = logEntry.Serialize();
        if (File.Exists(LogFilePath))
        {
            File.AppendAllText(LogFilePath, $",{data}");
        }
        else
        {
            File.AppendAllText(LogFilePath, data);

        }
    }
}
