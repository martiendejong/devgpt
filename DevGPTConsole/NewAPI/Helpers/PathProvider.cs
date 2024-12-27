namespace DevGPT.NewAPI
{
    public class PathProvider
    {
        protected string BasePath { get; set; }
        public PathProvider(string basePath = "")
        {
            BasePath = basePath;
        }

        public bool IsRelative => !string.IsNullOrWhiteSpace(BasePath);

        public string GetPath(string path)
        {
            return BasePath == "" ? path : $"{BasePath}\\{path}";
        }
    }
}