using System.Collections.Generic;
using System;

namespace Store.Model
{
    public class EmbeddingsFile
    {
        public string DocumentId { get; set; }
        public List<Embedding> Embeddings { get; set; } = new List<Embedding>();
        public DateTime LastUpdated { get; set; }
    }

    // This is expected by consumer code using EmbeddingI
    public class Embedding : Store.EmbeddingI
    {
        public string Type { get; set; }
        public float[] Values { get; set; }
        public string Source { get; set; } // E.g., sentence or word
    }
}
