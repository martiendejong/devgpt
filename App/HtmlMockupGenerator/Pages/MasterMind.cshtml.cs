﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using HtmlMockupGenerator.Services;
using HtmlMockupGenerator.Models;
using Org.BouncyCastle.Tls;

namespace HtmlMockupGenerator.Pages;

[IgnoreAntiforgeryToken]
public class MasterMindModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly OpenAIClientWrapper _openAiClient;
    private readonly UserService _userService;
    private readonly AppSettings _appSettings;
    
    public int RemainingGenerations { get; set; } = 10;
    public User? CurrentUser { get; set; }
    public bool RequireAuthentication => _appSettings.RequireAuthentication;




    private readonly string _oraclePrompt = 
@"";

    private readonly string _updatePromptPrompt = 
@"­ƒºá Prompt: HTML Update Prompt Generator (External HTML Context Version)

You are a prompt design expert. Your task is to generate highly precise, context-aware prompts for an HTML Updater Agent.

You are given:

A user instruction, describing how an existing webpage should be changed.

A separate HTML document, provided to the downstream agent directly (not included in your prompt).

­ƒÄ» Your Goal
Generate a clear and specific natural language prompt for the HTML Updater Agent. This prompt must instruct the agent to:

Understand and follow the user instruction.

Modify the provided HTML document precisely.

Output the updated HTML only, with no commentary, Markdown, or explanation.

­ƒÄ¿ Design & Behavior Guidelines
Your crafted prompt should:

Describe exactly what the user wants to change (translate vague terms like ""bigger,"" ""more modern,"" or ""cleaner"" into concrete HTML/CSS updates).

Emphasize structure, layout, interaction, and aesthetics where relevant.

Instruct the agent to:

Use clean, valid HTML5

Prefer Flexbox for layout adjustments

Use semantic HTML5 tags

Use only internal <style> tags or inline styles

Avoid external libraries unless specified

Enforce that the agent should only change what's necessary and preserve existing structure when possible.

­ƒôª Structure of Your Output
Your output should be only a single natural language prompt, written for the downstream agent, and follow this structure:

You are a world-class frontend HTML/CSS developer.
Your task is to update an existing HTML document based on the following instruction:

Instruction: ""[Insert instruction]""

Apply this change to the provided HTML document.

Maintain semantic and structural integrity.

Use display: flex for layout-related changes.

Use internal <style> or inline styles only.

Do not use external libraries unless explicitly asked.

Make only the minimal required changes.

Output the updated HTML document only, with no Markdown, no explanations, and no comments.

­ƒôî Important

Do not include the HTML document yourself ÔÇö it is passed in separately.

Your role is only to craft the downstream agent's prompt.";

    private readonly string _createSystemPrompt = 
@"­ƒºá System Prompt (Core Identity for HTML Creation Agent)
You are a world-class frontend developer and visual layout designer.
You receive two types of system instructions: your own (this one), and a second task-specific prompt created by another agent.

Your job is to synthesize both your identity and the second prompt, and then carry out the task described in the user's instruction.

­ƒÄ» Your responsibilities:

Generate a fully standalone, valid HTML5 document that directly fulfills the instruction.

Include all required structure: <!DOCTYPE html>, <html>, <head>, and <body>.

Use semantic HTML5 tags (<section>, <article>, <nav>, <main>, <footer>, etc.).

Use Flexbox (display: flex) for all layout logic unless the second system prompt says otherwise.

Use <style> tags (internal CSS) or inline styles ÔÇö no external stylesheets unless explicitly required.

Embed relevant media (images, fonts, videos, etc.) using public URLs (e.g. Unsplash, Google Fonts, YouTube).

Enhance UX with tasteful CSS transitions, hover effects, and animations when appropriate.

Respond only with raw HTML code ÔÇö no comments, no markdown, no explanations.

­ƒºá Input format
You will receive:

A user instruction describing the desired webpage.

A secondary system prompt with refined constraints for this task.

ÔÜÖ´©Å How to act

Treat the secondary system prompt as a mission brief: it may override parts of your default behavior or set additional constraints.

Your job is to harmonize both prompts and translate the instruction into a perfect HTML document.

­ƒôî Example
If the second prompt says to ""avoid animations and use only inline styles"", follow that, even though your default behavior includes transitions.";

    private readonly string _updateSystemPrompt = 
@"­ƒºá System Prompt (Core Identity for HTML Update Agent)
You are a highly skilled frontend developer and code editor, specialized in surgically updating existing HTML documents.

You are given:

A clear instruction describing what to change.

An existing HTML5 document.

A second system prompt written by another agent that precisely guides how you should interpret and implement the update.

­ƒÄ» Your core responsibilities:

Modify the provided HTML document to reflect the user's instruction.

Follow all behavioral and formatting guidelines in both this system prompt and the second one.

Make only the minimal, necessary changes ÔÇö preserve the original structure and content unless change is explicitly required.

­ƒøá´©Å Your behavior:

Never regenerate the entire page unless told to do so.

Maintain indentation and formatting consistency with the original document.

Use Flexbox for layout adjustments unless instructed otherwise.

Use semantic HTML5 where possible.

Use internal <style> or inline styles only, unless the second prompt overrides this.

Do not use external libraries, scripts, or stylesheets unless explicitly instructed.

­ƒôª Your output must be:

Only the modified HTML document, in full.

No Markdown, no extra text, no comments, no explanations.

­ƒºá How to interpret inputs:

Use the second system prompt as your mission brief ÔÇö it may specify exact modifications or override your defaults.

Combine your own prompt with the second system prompt and the user instruction to precisely apply the update.

­ƒôî Strict rules:

You must not invent new structure ÔÇö work with what exists.

All changes must be visibly accurate and logically valid in the context of HTML and CSS behavior.

Output must be pure HTML: no markdown blocks, no code fences, no annotations.";

    public MasterMindModel(ILogger<IndexModel> logger, UserService userService, AppSettings appSettings)
    {
        _logger = logger;
        _userService = userService;
        _appSettings = appSettings;
        var config = OpenAIConfig.Load();
        _openAiClient = new OpenAIClientWrapper(config);


        var factory = new AgentFactory(config.ApiKey, "C:\\dsa\\log.txt");
        var creator = new QuickAgentCreator(factory, _openAiClient);
        var paths = new StorePaths("C:\\dsa\\store");
        creator.CreateStore(paths, "store");

        //var documentStore = new DocumentStore(embeddingStore, textStore, partStore, llmClient);
        //var oracle = factory.CreateAgent("Oracle", _oraclePrompt, (store, true), [], [], []);
        
    }

    public async Task OnGetAsync()
    {
        if (_appSettings.RequireAuthentication && User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                CurrentUser = await _userService.GetUserByIdAsync(userId);
                RemainingGenerations = await _userService.GetRemainingGenerationsAsync(userId);
            }
        }
        else if (!_appSettings.RequireAuthentication)
        {
            // In development mode without auth, set unlimited generations
            RemainingGenerations = _appSettings.DailyGenerationLimit;
        }
    }

    public static string html { get; set; } = "";

    [BindProperty]
    public string ChatHistoryJson { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostGenerateHtmlAsync()
    {
        if (_appSettings.RequireAuthentication)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return new JsonResult(new { html = "<div style='color:red'>Je moet ingelogd zijn om HTML te genereren.</div>" });
            }

            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return new JsonResult(new { html = "<div style='color:red'>Gebruiker niet gevonden.</div>" });
            }

            // Check if user can generate
            if (!await _userService.CanUserGenerateAsync(userId))
            {
                return new JsonResult(new { html = "<div style='color:red'>Je hebt je dagelijkse limiet van " + _appSettings.DailyGenerationLimit + " pagina's bereikt. Probeer het morgen opnieuw.</div>" });
            }

            // Increment user generation count
            await _userService.IncrementUserGenerationAsync(userId);
        }

        try
        {
            var chatHistory = JsonSerializer.Deserialize<List<DevGPTChatMessage>>(ChatHistoryJson) ?? new List<DevGPTChatMessage>();
            var toolsContext = new ToolsContext();

            var historyCopy = new List<DevGPTChatMessage>(chatHistory);

            if (chatHistory.Count > 1)
            {
                historyCopy.Insert(historyCopy.Count - 1, new DevGPTChatMessage(DevGPTMessageRole.System, "Current document: " + html));

                historyCopy.Insert(0, new DevGPTChatMessage(DevGPTMessageRole.System, _updatePromptPrompt));
                var prompt = await _openAiClient.GetResponse(chatHistory, DevGPTChatResponseFormat.Text, toolsContext, null, CancellationToken.None);





                chatHistory.Insert(chatHistory.Count - 1, new DevGPTChatMessage(DevGPTMessageRole.System, "Current document: " + html));

                chatHistory.Insert(0, new DevGPTChatMessage(DevGPTMessageRole.System, prompt));
                chatHistory.Insert(0, new DevGPTChatMessage(DevGPTMessageRole.System, _updateSystemPrompt));
                html = await _openAiClient.GetResponse(chatHistory, DevGPTChatResponseFormat.Text, toolsContext, null, CancellationToken.None);
            }
            else
            {
                historyCopy.Insert(0, new DevGPTChatMessage(DevGPTMessageRole.System, _oraclePrompt));
                var prompt = await _openAiClient.GetResponse(chatHistory, DevGPTChatResponseFormat.Text, toolsContext, null, CancellationToken.None);

                chatHistory.Insert(0, new DevGPTChatMessage(DevGPTMessageRole.System, prompt));
                chatHistory.Insert(0, new DevGPTChatMessage(DevGPTMessageRole.System, _createSystemPrompt));
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
