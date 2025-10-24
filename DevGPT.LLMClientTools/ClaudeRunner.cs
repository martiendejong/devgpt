using System.Diagnostics;
using System.Text;

public static class ClaudeRunner
{
    public static async Task<string> RunClaudeAsync(string prompt, string? model = null, string? extraArgs = null, int timeoutSeconds = 120, CancellationToken cancel = default)
    {
        var argsBuilder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(model)) argsBuilder.Append($"--model \"{EscapeArg(model)}\" ");
        if (!string.IsNullOrWhiteSpace(extraArgs)) argsBuilder.Append(extraArgs.Trim()).Append(' ');
        argsBuilder.Append($"--message \"{EscapeArg(prompt)}\" ");

        return await RunProcessAsync("claude", argsBuilder.ToString().Trim(), TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)), cancel);
    }

    private static string EscapeArg(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static async Task<string> RunProcessAsync(string fileName, string arguments, TimeSpan timeout, CancellationToken cancel)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var tcs = new TaskCompletionSource<object?>();

        proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        if (!proc.Start())
            return "Failed to start 'claude' process.";

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        cts.CancelAfter(timeout);

        await Task.WhenAny(WaitForExitAsync(proc, cts.Token));

        if (!proc.HasExited)
        {
            try { proc.Kill(true); } catch { }
            return $"Process timed out after {timeout.TotalSeconds} seconds.\n" + stdout.ToString().TrimEnd() + (stderr.Length > 0 ? "\n[stderr]\n" + stderr.ToString().TrimEnd() : "");
        }

        return stdout.ToString().TrimEnd() + (stderr.Length > 0 ? "\n[stderr]\n" + stderr.ToString().TrimEnd() : "");
    }

    private static Task WaitForExitAsync(Process proc, CancellationToken token)
    {
        var tcs = new TaskCompletionSource<object?>();
        void Handler(object? s, EventArgs e)
        {
            tcs.TrySetResult(null);
            try { proc.Exited -= Handler; } catch { }
        }
        proc.Exited += Handler;
        if (proc.HasExited) Handler(proc, EventArgs.Empty);
        token.Register(() => tcs.TrySetCanceled(token));
        return tcs.Task;
    }
}

