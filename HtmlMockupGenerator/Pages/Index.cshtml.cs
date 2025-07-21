using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace HtmlMockupGenerator.Pages;

[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly OpenAIClientWrapper _openAiClient;
    private readonly string _systemPrompt = "Je bent een HTML mockup generator. Op basis van de volledige chatgeschiedenis en het laatste bericht van de gebruiker genereer je een volledige HTML-pagina die het beste past bij de wensen van de gebruiker. Geef alleen geldige, complete HTML terug, zonder uitleg of extra tekst.";

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
        var config = OpenAIConfig.Load();
        _openAiClient = new OpenAIClientWrapper(config);
    }

    public void OnGet()
    {
    }

    [BindProperty]
    public string ChatHistoryJson { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostGenerateHtmlAsync()
    {
        try
        {
            var chatHistory = JsonSerializer.Deserialize<List<DevGPTChatMessage>>(ChatHistoryJson) ?? new List<DevGPTChatMessage>();
            // Voeg de system prompt toe als eerste bericht
            chatHistory.Insert(0, new DevGPTChatMessage(DevGPTMessageRole.System, _systemPrompt));
            var toolsContext = new ToolsContextBase();
            var html = await _openAiClient.GetResponse(chatHistory, DevGPTChatResponseFormat.Text, toolsContext, null, CancellationToken.None);
            return new JsonResult(new { html });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij genereren HTML");
            return new JsonResult(new { html = "<div style='color:red'>Fout bij genereren van HTML: " + ex.Message + "</div>" });
        }
    }
}
