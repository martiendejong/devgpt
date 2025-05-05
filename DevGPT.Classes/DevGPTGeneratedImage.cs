
namespace Store.OpnieuwOpnieuw.AIClient
{
    public class DevGPTGeneratedImage
    {
        public DevGPTGeneratedImage(string url, BinaryData imageBytes)
        {
            Url = url;
            ImageBytes = imageBytes;
        }
        public string Url { get; }
        public BinaryData ImageBytes { get; }
    }
}