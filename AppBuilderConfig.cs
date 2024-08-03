namespace DevGPT
{
    public class AppBuilderConfig
    {
        public string ProjectName { get; set; } = "";
        public string FolderPath { get; set; } = "";
        public string EmbeddingsFile { get; set; } = "";
        public string HistoryFile { get; set; } = "";
        public bool GenerateEmbeddings { get; set; }
        public bool UseHistory { get; set; }
        public string Query { get; set; } = "";

        // New prompt fields
        public string SystemInstructions1 { get; set; } = "You are an assistant with the goal of creation a software application.\nYour data source will be a list of all files that are currently in the project and their contents.\nAnswer the question of the user as best as you can.";
        public string SystemInstructions2 { get; set; } = "You are an assistant with the goal of creation a software application.\nYour data source will be a list of all files that are currently in the project and their contents.\nThe user will task you with writing a component.\nYou will provide the necessary code changes and a description of those changes.\nThen the compiler will write you the compilation result. If the result is an error, you will provide code changes to solve the error.";
    }
}
