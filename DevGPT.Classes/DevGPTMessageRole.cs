#nullable enable

public class DevGPTMessageRole
{
    public string Role { get; }
    protected DevGPTMessageRole(string role) => Role = role;
    public static readonly DevGPTMessageRole User = new DevGPTMessageRole("user");
    public static readonly DevGPTMessageRole System = new DevGPTMessageRole("system");
    public static readonly DevGPTMessageRole Assistant = new DevGPTMessageRole("assistant");
}
