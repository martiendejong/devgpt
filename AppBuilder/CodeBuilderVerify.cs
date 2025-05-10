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

public class CodeBuilderContinuous : ChatResponse<CodeBuilderContinuous>
{
    public bool Finished { get; set; }
    public string Message { get; set; }

    [JsonIgnore]
    public override CodeBuilderContinuous _example => new CodeBuilderContinuous
    {
        Finished = false,
        Message = "It seems there is no git branch for the feature. Let me create it."
    };

    [JsonIgnore]
    public override string _signature => @"{ Finished: bool, Message: string }";
}
