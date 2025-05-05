
namespace Store.OpnieuwOpnieuw.AIClient
{
    public class DevGPTGeneratedImage
    {
        public DevGPTGeneratedImage(BinaryData imageBytes)
        {
            ImageBytes = imageBytes;
        }

        public BinaryData ImageBytes { get; }
    }
}