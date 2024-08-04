using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DevGPT;

public class CodeUpdater
{
    private AppBuilderConfig config;

    public CodeUpdater(AppBuilderConfig config)
    {
        this.config = config;
    }

    public async Task<string> UpdateProject(Response result)
    {
        result.Changes.ForEach(item =>
        {
            var filePath = $@"{config.FolderPath}\{item.File}";
            var file = new FileInfo(filePath);
            if (!file.Directory.Exists)
            {
                Directory.CreateDirectory(file.Directory.FullName);
            }
            File.WriteAllText($@"{config.FolderPath}\{item.File}", item.Content);
        });
        result.Deletions.ForEach(item =>
        {
            File.Delete($@"{config.FolderPath}\{item}");
        });
        return result.Message;
    }
}