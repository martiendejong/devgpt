// See https://aka.ms/new-console-template for more information
using DevGPT.NewAPI;
using System.Text.Json.Serialization;

public class CodeBuilderTasks : ChatResponse<CodeBuilderTasks>
{
    public List<CodeBuilderTask> Tasks { get; set; }

    [JsonIgnore]
    public override CodeBuilderTasks _example => new CodeBuilderTasks { Tasks = new List<CodeBuilderTask> { 
        new CodeBuilderTask {
            Title = "Analyze the example JSON", 
            Description = "The example JSON shows what a proper response looks like.",
        },
        new CodeBuilderTask {
            Title = "Analyze the class signature",
            Description = "The class signature shows the properties and their datatypes, and if the property is optional or required.",
            Status = "testing"
        },
    } };

    [JsonIgnore]
    public override string _signature => @"{ Tasks: { Title: string, Description: string, Status: string }[] } // Status is todo or test or done";
}
