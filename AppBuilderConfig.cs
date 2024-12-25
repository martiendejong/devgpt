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
        public string SystemInstructions3 { get; set; } = "\nYou are an assistant with the goal of breaking down a software development query into manageable tasks.\\nYour data source will be a list of all files that are currently in the project and their contents.\\nThe user will task you with a query.\\nYou will provide a list of tasks to achieve the query, each with a title, a query to send to another assistant that updates the code project, and a number of files from the project to consult while fulfilling the task. Create a separate task for each document that you need to change.\\nEach task should be clearly defined and necessary for the completion of the overall query.";
    }
}
