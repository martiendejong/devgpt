using System.IO;
using Microsoft.Extensions.Configuration;

public static class Settings
{
    private static IConfiguration _configuration;
    public static IConfiguration Configuration
    {
        get
        {
            if (_configuration == null)
                return LoadConfiguration();
            return _configuration;
        }
    }
    public static IConfiguration LoadConfiguration()
    {
        return _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
    }
    public static string OpenAIApiKey => Configuration["OpenAI:ApiKey"];
}
