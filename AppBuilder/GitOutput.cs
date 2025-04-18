﻿// See https://aka.ms/new-console-template for more information
using System.Diagnostics;

public static class GitOutput
{
    public static Tuple<string, string> GetGitOutput(string workingDirectory, string arguments)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false, // Required to redirect output
            CreateNoWindow = true // Optional: prevent console window
        };

        using (Process process = new Process { StartInfo = psi })
        {
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            return new Tuple<string, string>(output, error);
        }
    }
}
