using Microsoft.Extensions.Configuration;

public class OpenAISettings
{
    public string ApiKey { get; set; }
    public static OpenAISettings Load()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory) // current directory
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Bind the OpenAI section to a strongly-typed object
        var openAISettings = new OpenAISettings();
        config.GetSection("OpenAI").Bind(openAISettings);
        string openAiApiKey = openAISettings.ApiKey;
        return openAISettings;
    }
}