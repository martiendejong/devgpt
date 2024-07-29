using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp1
{
    public class ProjectLoader
    {
        public string[] ExcludeNames = new[] { @"\bin\", @"\obj\", @"\node_modules\", ".git", ".vs", "dist", ".d.ts", "appsettings.json" };
        public string[] FileTypes = new[] { ".cs", ".xaml", ".xaml.cs", ".cshtml", ".html", ".json", ".js", ".ts", ".tt", ".md" };

        public List<string> GetAllProjectFiles(string folderPath)
        {
            var allFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Where(file => !ExcludeNames.Any(f => file.Contains(f)) && (FileTypes.Any(f => file.EndsWith(f))))
                .ToList();
            return allFiles;
        }
    }
}