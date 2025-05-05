namespace Store.OpnieuwOpnieuw
{
    public class OpenAIConfig
    {
        public string ApiKey { get; set; }
        public string Model { get; set; } = "gpt-4.1";//"gpt-4o";
        public string ImageModel { get; set; } = "gpt-image-1";//"dall-e-3";
        public string EmbeddingModel { get; set; } = "text-embedding-ada-002";
    }
}
