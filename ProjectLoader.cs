using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace DevGPT
{
    public class ProjectLoader
    {
        private readonly string[] excludeDirs = new[] { @"\bin\", @"\obj\", @"\node_modules\", ".git", ".vs", "dist", ".d.ts", "appsettings.json", ".quasar" };
        private readonly string[] includeExtensions = new[] { ".cs", ".xaml", ".xaml.cs", ".cshtml", ".html", ".json", ".js", ".ts", ".tt", ".md" };

        public List<string> GetFilesRelative(string folderPath) => GetFiles.Select(file => Path.GetRelativePath(folderPath, file);
        public List<string> GetFiles(string folderPath) => Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Where(file => !excludeDirs.Any(excludeDir => file.Contains(excludeDir)) && includeExtensions.Any(ext => file.EndsWith(ext)))
                .ToList();
    }
}