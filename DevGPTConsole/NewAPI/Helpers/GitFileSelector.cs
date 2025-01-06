using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;

public class GitFileSelector
{
    public static List<string> GetMatchingFiles(string folderPath, List<string> fileSelectors)
    {
        if (!Repository.IsValid(folderPath))
        {
            throw new ArgumentException("The provided folder is not a valid Git repository.");
        }

        using (var repo = new Repository(folderPath))
        {
            // Retrieve all tracked files in the repository
            var allTrackedFiles = repo.Index
                                      .Select(entry => entry.Path)
                                      .ToList();

            // Separate positive and negative selectors
            var positiveSelectors = fileSelectors.Where(s => !s.StartsWith("!")).ToList();
            var negativeSelectors = fileSelectors.Where(s => s.StartsWith("!"))
                                                 .Select(s => s.Substring(1)) // Remove '!'
                                                 .ToList();

            var matchingFiles = new HashSet<string>();

            // Apply positive selectors
            foreach (var selector in positiveSelectors)
            {
                string basePath = Path.GetDirectoryName(selector) ?? string.Empty;
                string pattern = Path.GetFileName(selector) ?? "*";

                var matched = allTrackedFiles.Where(f =>
                    (string.IsNullOrEmpty(basePath) || f.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(pattern) || MatchesPattern(Path.GetFileName(f), pattern)));

                foreach (var file in matched)
                {
                    matchingFiles.Add(file);
                }
            }

            // Apply negative selectors
            foreach (var selector in negativeSelectors)
            {
                string basePath = Path.GetDirectoryName(selector) ?? string.Empty;
                string pattern = Path.GetFileName(selector) ?? "*";

                var excluded = allTrackedFiles.Where(f =>
                    (string.IsNullOrEmpty(basePath) || f.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(pattern) || MatchesPattern(Path.GetFileName(f), pattern)));

                foreach (var file in excluded)
                {
                    matchingFiles.Remove(file);
                }
            }

            return matchingFiles.ToList();
        }
    }

    private static bool MatchesPattern(string fileName, string pattern)
    {
        // Convert wildcard pattern to regex
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    public static void Main(string[] args)
    {
        string folderPath = "/path/to/your/git/repository";
        var fileSelectors = new List<string> { "*.txt", "documents/*.pdf", "!documents/excluded-file.pdf" };

        try
        {
            var files = GetMatchingFiles(folderPath, fileSelectors);
            Console.WriteLine("Matching files:");
            foreach (var file in files)
            {
                Console.WriteLine(file);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}