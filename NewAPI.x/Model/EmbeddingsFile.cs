﻿using System.IO;



namespace DevGPT.NewAPI
{
    public class EmbeddingsFile
    {
        public string Path { get; set; }

        public EmbeddingsFile(string path)
        {
            Path = path;
        }

        public void Save(List<Embedding> embeddings)
        {
            var lines = embeddings.Select(e => $"{EscapeCommaAndNewLine(e.Name)},{e.Path},{e.Checksum},{string.Join(",", e.Embeddings)}");
            var text = string.Join("\n", lines);
            File.WriteAllText(Path, text);
        }

        private static string EscapeCommaAndNewLine(string name)
        {
            return name.Replace("\n", "").Replace(",", "&comma;");
        }

        private static string UnescapeCommaAndNewLine(string name)
        {
            return name.Replace("&comma;", ",");
        }

        public List<Embedding> Load()
        {
            var lines = File.ReadAllLines(Path);
            var embeddings = lines.Select(line =>
            {
                var values = line.Split(",");
                var name = UnescapeCommaAndNewLine(values[0]);
                var path = values[1];
                var checksum = values[2];
                var data = values.Skip(3).Select(e => double.Parse(e)).ToList();
                var embedding = new Embedding(name, path, checksum, new EmbeddingData(data));
                return embedding;
            }).ToList();
            return embeddings;            
        }
    }
}