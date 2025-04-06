// See https://aka.ms/new-console-template for more information
using DevGPT.NewAPI;
using System.Text.Json.Serialization;

public class CodeBuilderVerify : ChatResponse<CodeBuilderVerify>
{
    public bool HasRework { get; set; }
    public string Rework { get; set; }

    [JsonIgnore]
    public override CodeBuilderVerify _example => new CodeBuilderVerify
    {
        HasRework = true,
        Rework = "There is an unused variable in the code."
    };

    [JsonIgnore]
    public override string _signature => @"{ hasRework: bool, rework: string }";
}
