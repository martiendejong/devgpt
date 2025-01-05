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

        var matchingFiles = new List<string>();

        using (var repo = new Repository(folderPath))
        {
            var allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                                    .Select(f => Path.GetRelativePath(folderPath, f).Replace("\\", "/"));

            foreach (var selector in fileSelectors)
            {
                // Handle directory-based selectors
                string basePath = Path.GetDirectoryName(selector) ?? string.Empty;
                string pattern = Path.GetFileName(selector) ?? "*";

                var matched = allFiles.Where(f =>
                    f.StartsWith(basePath, StringComparison.OrdinalIgnoreCase) &&
                    (Path.GetFileName(f)?.EndsWith(pattern, StringComparison.OrdinalIgnoreCase) ?? false));

                matchingFiles.AddRange(matched);
            }
        }

        // Deduplicate the result
        return matchingFiles.Distinct().ToList();
    }

    public static void Main(string[] args)
    {
        string folderPath = "/path/to/your/git/repository";
        var fileSelectors = new List<string> { "*.txt", "documents/*.pdf" };

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