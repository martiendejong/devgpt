using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}

static int Skip(string message)
{
    Console.WriteLine(message);
    return 2;
}

async Task<int> Run()
{
    var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    // Fallback to appsettings.json via OpenAIConfig if env var missing
    OpenAIConfig? loaded = null;
    try { loaded = OpenAIConfig.Load(); } catch { /* appsettings may be missing */ }
    if (string.IsNullOrWhiteSpace(apiKey)) apiKey = loaded?.ApiKey;
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return Skip("No API key found in environment or appsettings.json. Skipping integration test.");
    }

    var tmpLog = Path.Combine(Path.GetTempPath(), "devgpt_image_integration_test_log.txt");
    var config = new OpenAIConfig(
        apiKey: apiKey!,
        embeddingModel: loaded?.EmbeddingModel ?? "text-embedding-3-small",
        model: loaded?.Model ?? "gpt-4.1-mini",
        imageModel: loaded?.ImageModel ?? "gpt-image-1",
        logPath: loaded?.LogPath ?? tmpLog
    );

    var wrapper = new OpenAIClientWrapper(config);

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

    Console.WriteLine("=== Test 1: Basic image generation ===");
    var image = await wrapper.GetImage(
        prompt: "a simple red square icon on white background",
        responseFormat: DevGPTChatResponseFormat.Text,
        toolsContext: null,
        images: null,
        cancel: cts.Token
    );

    if (image == null) return Fail("Image result is null");

    var urlPresent = image.Url != null && !string.IsNullOrWhiteSpace(image.Url.ToString());
    var bytes = image.ImageBytes?.ToArray() ?? Array.Empty<byte>();
    var bytesPresent = bytes.Length > 0;

    if (!urlPresent && !bytesPresent)
        return Fail("Neither image URL nor bytes were provided");

    if (bytesPresent)
    {
        bool looksPng = bytes.Length > 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47; // PNG
        bool looksJpeg = bytes.Length > 3 && bytes[0] == 0xFF && bytes[1] == 0xD8; // JPEG

        if (!looksPng && !looksJpeg)
            Console.WriteLine("WARN: Image bytes do not appear to be PNG or JPEG");
    }

    Console.WriteLine("PASS: Image generation returned valid output");
    Console.WriteLine($"URL present: {urlPresent}, Bytes present: {bytesPresent}");
    if (urlPresent) Console.WriteLine($"URL: {image.Url}");

    // Test 2: Test DevGPTGeneratedImage with only ImageBytes
    Console.WriteLine("\n=== Test 2: DevGPTGeneratedImage with only ImageBytes ===");
    {
        // Create a mock image response with only bytes
        var mockBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG header
        var mockImage = new DevGPTGeneratedImage(null, BinaryData.FromBytes(mockBytes));

        if (mockImage.Url != null)
            return Fail("Mock image should have null URL");

        if (mockImage.ImageBytes == null || mockImage.ImageBytes.ToArray().Length == 0)
            return Fail("Mock image should have bytes");

        Console.WriteLine("PASS: DevGPTGeneratedImage handles null URL with bytes correctly");
    }

    // Test 3: Test DevGPTGeneratedImage with only URL
    Console.WriteLine("\n=== Test 3: DevGPTGeneratedImage with only URL ===");
    {
        var mockUrl = new Uri("https://example.com/image.png");
        var mockImage = new DevGPTGeneratedImage(mockUrl, null);

        if (mockImage.Url == null)
            return Fail("Mock image should have URL");

        if (mockImage.ImageBytes != null && mockImage.ImageBytes.ToArray().Length > 0)
            return Fail("Mock image should have null bytes");

        Console.WriteLine("PASS: DevGPTGeneratedImage handles null bytes with URL correctly");
    }

    // Optional: vision check by sending the generated image back to the chat model
    try
    {
        if (!bytesPresent)
        {
            Console.WriteLine("INFO: Skipping vision check since image bytes are not available");
            return 0;
        }

        // Heuristic MIME check
        var looksPng = bytes.Length > 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;
        var mime = looksPng ? "image/png" : "image/jpeg";
        var testImage = new ImageData { Name = "generated", MimeType = mime, BinaryData = image.ImageBytes };
        var messages = new List<DevGPTChatMessage>
        {
            new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = "You are a helpful assistant." },
            new DevGPTChatMessage { Role = DevGPTMessageRole.User, Text = "What primary color is dominant in the attached image? Answer 'red', 'green', or 'blue' only." }
        };
        var visionAnswer = await wrapper.GetResponse(messages, DevGPTChatResponseFormat.Text, toolsContext: null, images: new() { testImage }, cancel: cts.Token);
        Console.WriteLine($"Vision answer: {visionAnswer}");
        if (visionAnswer?.ToLowerInvariant().Contains("red") == true)
        {
            Console.WriteLine("PASS: Vision check matched expected 'red'");
        }
        else
        {
            Console.WriteLine("WARN: Vision check did not match 'red'");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"WARN: Vision check failed with exception: {ex.Message}");
    }

    return 0;
}

return await Run();
