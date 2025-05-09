using System.Collections.Generic;
using System.Linq;
using Store.Model;
using DevGPT.Helpers.Embedding;

namespace Store.Helpers
{
    public class RelevantDocumentsProvider
    {
        private readonly IEnumerable<EmbeddingsFile> _embeddingFiles;
        public RelevantDocumentsProvider(IEnumerable<EmbeddingsFile> embeddingFiles)
        {
            _embeddingFiles = embeddingFiles;
        }

        public IEnumerable<EmbeddingsFile> GetRelevant(string query, int maxCount = 5)
        {
            var queryEmbeddings = EmbeddingGenerator.GenerateEmbeddings(query);
            // Very simple cosine similarity ranking for demonstration
            return _embeddingFiles.Select(f => new {
                File = f,
                Score = ScoreFile(f, queryEmbeddings)
            })
            .OrderByDescending(x => x.Score)
            .Take(maxCount)
            .Select(x => x.File);
        }
        private static float ScoreFile(EmbeddingsFile file, List<Embedding> queryEmbeddings)
        {
            if (file.Embeddings == null || queryEmbeddings == null || file.Embeddings.Count == 0) return 0f;
            // Compare first vector for demo
            var v1 = file.Embeddings[0].Values;
            var v2 = queryEmbeddings[0].Values;
            return CosineSimilarity(v1, v2);
        }
        private static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length) return 0;
            float dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i]; magA += a[i] * a[i]; magB += b[i] * b[i];
            }
            if (magA == 0 || magB == 0) return 0;
            return dot / (float)(System.Math.Sqrt(magA) * System.Math.Sqrt(magB));
        }
    }
}
