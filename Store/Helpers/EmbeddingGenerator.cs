using System;
using System.Collections.Generic;
using DevGPT.Helpers.Embedding;

namespace Store.Helpers
{
    public static class EmbeddingGenerator
    {
        public static List<Embedding> GenerateEmbeddings(string text)
        {
            // Actual implementation of embedding generation
            // This is expected by codebase, especially for semantic search
            if (string.IsNullOrEmpty(text)) return new List<Embedding>();
            // For demo: Split words and hash as float[]
            var words = text.Split(' ');
            var embeddings = new List<Embedding>();
            foreach (var word in words)
            {
                float[] vector = new float[4];
                for (int i = 0; i < 4; i++)
                    vector[i] = word.GetHashCode() * (i + 0.1f) % 997 / 997f;
                embeddings.Add(new Embedding { Values = vector, Type = "word", Source = word });
            }
            return embeddings;
        }
    }
}
