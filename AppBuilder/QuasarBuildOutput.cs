// See https://aka.ms/new-console-template for more information
using System.Diagnostics;

public static class QuasarBuildOutput
{
    public static string buildFile = "runquasar.bat";
    public static string buildOutputFile = "build.log";
    public static string buildErrorsFile = "errors.log";

    public static Tuple<string, string> GetQuasarBuildOutput(string workingDirectory)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            WorkingDirectory = workingDirectory,
            FileName = workingDirectory + "\\" + buildFile,
            //Arguments = "build",
            //RedirectStandardOutput = true,
            //RedirectStandardError = true,
            UseShellExecute = true
        };

        using (Process process = new Process { StartInfo = psi })
        {
            process.Start();

            // Read output and errors
            //string output = process.StandardOutput.ReadToEnd();
            //string error = process.StandardError.ReadToEnd();
            //var output = "";
            //var error = "";

            process.WaitForExit();

            var output = File.ReadAllText(workingDirectory + "\\" + buildOutputFile);
            var error = File.ReadAllText(workingDirectory + "\\" + buildErrorsFile);

            return new Tuple<string, string>(output, error);
        }
    }
}
