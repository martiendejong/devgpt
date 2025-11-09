using Microsoft.Extensions.Configuration;

namespace DevGPT.LLMs.Gemini;

public class GeminiConfig
{
    public GeminiConfig(string apiKey = "", string model = "gemini-1.5-pro", string endpoint = "https://generativelanguage.googleapis.com/v1beta", string logPath = "c:\\projects\\devgptlogs.txt")
    {
        ApiKey = apiKey;
        Model = model;
        Endpoint = endpoint.TrimEnd('/');
        LogPath = logPath;
    }

    public string ApiKey { get; set; }
    public string Model { get; set; }
    public string Endpoint { get; set; }
    public string LogPath { get; set; }

    public static GeminiConfig Load()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var settings = new GeminiConfig();
        config.GetSection("Gemini").Bind(settings);
        return settings;
    }
}

