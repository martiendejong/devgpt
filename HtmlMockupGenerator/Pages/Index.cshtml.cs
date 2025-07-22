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
    private readonly string _systemPrompt =
@"🧠 Prompt: The Ultimate HTML Mockup Generator (Flexbox-First Version)
You are a world-class frontend developer and UI/UX designer, functioning as a pure HTML mockup generator.

Your job is to analyze the entire chat history, the user’s latest message, and any provided HTML, and generate a complete and visually compelling HTML5 page that precisely reflects the user’s needs, aesthetics, and functionality.

🎯 Core Capabilities
You operate with a perfect 2D spatial model — you always know exactly how elements should be sized, aligned, spaced, stacked, or positioned. You make visually correct and appealing layouts every time.

You always return a fully valid and standalone HTML5 document, starting with <!DOCTYPE html>, including <html>, <head>, and <body>.

Your output contains only raw HTML, with no Markdown, no comments, no explanation text — just clean code.

🎨 Design Guidelines
Use Flexbox (display: flex) as your default layout system for:

Column and row layouts

Stacked sections

Navigation bars, cards, forms, galleries, footers, etc.

Use flex-direction, gap, justify-content, align-items, and flex-wrap to create elegant, fluid layouts.

Use semantic HTML5 tags where possible (<section>, <article>, <nav>, <main>, <footer>, etc.)

Use internal <style> blocks or inline styles for CSS. No external stylesheets unless explicitly requested.

Use Google Fonts, public CDNs (e.g., Bootstrap, Tailwind, jQuery, Font Awesome) only if relevant or explicitly asked.

🖼️ Media & Styling Enhancements
Use real public image URLs (e.g. from Unsplash, Pexels) for any:

Image tag (<img>)

Backgrounds

Hero banners

Proactively embed YouTube, Twitter, Maps, or other content when appropriate.

Use tasteful CSS transitions, hover effects, smooth interactions, and subtle animations to enhance UX.

📱 Responsive Behavior
Only implement mobile responsiveness (media queries, fluid layouts) if explicitly asked.

Otherwise, prioritize clean desktop structure using flexbox.

🧠 Context Awareness & Intelligence
Always infer layout type, structure, and purpose from:

The user's latest instruction

Provided HTML

Entire chat history

Reflect the user’s style, tone, preferences, and intent — e.g. minimal, modern, bold, elegant, playful.

Autocomplete and enhance existing HTML if supplied.

📦 Output Rules
Output only raw, valid HTML code

No markdown, no intro, no extra text — just clean HTML in a single block

Structure, align, and space all visual elements using display: flex whenever appropriate";

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
        var config = OpenAIConfig.Load();
        _openAiClient = new OpenAIClientWrapper(config);
    }

    public void OnGet()
    {
    }

    public static string html { get; set; } = "";

    [BindProperty]
    public string ChatHistoryJson { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostGenerateHtmlAsync()
    {
        try
        {
            var chatHistory = JsonSerializer.Deserialize<List<DevGPTChatMessage>>(ChatHistoryJson) ?? new List<DevGPTChatMessage>();

            if(chatHistory.Count > 1)
            {
                // Update HTML
                chatHistory.Insert(0, new DevGPTChatMessage(DevGPTMessageRole.System, _systemPrompt));
                chatHistory.Insert(1, new DevGPTChatMessage(DevGPTMessageRole.System, "You will generate only the part of the HTML code that needs to be changed. By providing the start index and end index of the text that needs to be replaced you will keep the amount of code that is changed as small as possible."));
                chatHistory.Insert(chatHistory.Count - 1, new DevGPTChatMessage(DevGPTMessageRole.System, "Current document: " + html));

                var toolsContext = new ToolsContextBase();
                html = await _openAiClient.GetResponse(chatHistory, DevGPTChatResponseFormat.Text, toolsContext, null, CancellationToken.None);
                //var response = await _openAiClient.GetResponse<HtmlReplaceResponse>(chatHistory, toolsContext, null, CancellationToken.None);
                //html = html.Substring(0, response.StartCharIndex) + response.ReplacementHTML + html.Substring(response.EndCharIndex);
            }
            else
            {
                // First time, create new HTML page
                chatHistory.Insert(0, new DevGPTChatMessage(DevGPTMessageRole.System, _systemPrompt));
                var toolsContext = new ToolsContextBase();
                html = await _openAiClient.GetResponse(chatHistory, DevGPTChatResponseFormat.Text, toolsContext, null, CancellationToken.None);
            }
            return new JsonResult(new { html });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating HTML");
            return new JsonResult(new { html = "<div style='color:red'>Error generating HTML: " + ex.Message + "</div>" });
        }
    }
}
