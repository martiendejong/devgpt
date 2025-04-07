using OpenAI.Chat;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace DevGPT.NewAPI
{
    public class RelevantDocumentsProvider
    {
        protected EmbeddingGenerator EmbeddingGenerator { get; set; }
        
        protected TokenCounter TokenCounter { get; set; }

        protected PathProvider PathProvider { get; set; }

        public RelevantDocumentsProvider(EmbeddingGenerator embeddingGenerator, PathProvider pathProvider)
        {
            EmbeddingGenerator = embeddingGenerator;
            TokenCounter = new TokenCounter();
            PathProvider = pathProvider;
        }

        public async Task<string> GetRelevantDocumentsAsString(string query, List<Embedding> embeddings)
        {
            List<string> selectedDocuments = await GetRelevantDocuments(query, embeddings);

            // Return document names as a comma-separated string
            //return string.Join(", ", selectedDocuments.Select(d => d.Name));
            return string.Join("\n\n", selectedDocuments);
        }

        public async Task<List<ChatMessage>> GetRelevantDocumentsAsChatMessages(string query, List<Embedding> embeddings)
        {
            List<string> selectedDocuments = await GetRelevantDocuments(query, embeddings);
            return selectedDocuments.Select(d => new AssistantChatMessage(d) as ChatMessage).ToList();
        }

        public async Task<List<string>> GetRelevantDocuments(string query, List<Embedding> embeddings)
        {
            int maxTokens = 20000;
            int maxInputTokens = 8000;

            // if query is too many tokens we keep cutting at the front because with chat messages the end is what matters
            var tokens = TokenCounter.CountTokens(query);
            while (tokens > maxInputTokens) {
                double divider = (double)tokens / (double)maxInputTokens;
                var x = query.Length / divider;
                var y = (int)x;
                int newLength = (int)x;
                query = query.Substring(query.Length - newLength);
                tokens = TokenCounter.CountTokens(query);
            }

            var documents = await GetDocumentsWithSimilarity(query, embeddings);

            // List to store selected documents
            var selectedDocuments = new List<string>();
            int currentTokenCount = 0;

            foreach (var document in documents)
            {
                var path = PathProvider.GetPath(document.document.Path);
                var text = File.ReadAllText(path);

                // Count tokens for the current document
                int documentTokenCount = TokenCounter.CountTokens(text);

                // Check if adding this document would exceed max tokens
                if (currentTokenCount + documentTokenCount <= maxTokens)
                {
                    var documentView = $"File name: {document.document.Name}\nFile path: {document.document.Path}\nFile content:\n{text}";
                    selectedDocuments.Add(documentView);
                    currentTokenCount += documentTokenCount;
                }
                else
                {
                    // Stop if adding the document would exceed max tokens
                    break;
                }
            }

            // Log token count and selected documents for debugging
            //Console.WriteLine($"Total Documents Selected: {selectedDocuments.Count}");
            //Console.WriteLine($"Total Token Count: {currentTokenCount}");
            return selectedDocuments;
        }

        public async Task<List<(double similarity, Embedding document)>> GetDocumentsWithSimilarity(string query, List<Embedding> embeddings)
        {
            var queryEmbeddingData = await EmbeddingGenerator.FetchEmbedding(query);

            var similarities = new List<(double similarity, Embedding document)>();

            foreach (var document in embeddings)
            {
                var similarity = queryEmbeddingData.CosineSimilarity(document.Embeddings);

                similarities.Add((similarity, document));
            }

            similarities.Sort((x, y) => y.similarity.CompareTo(x.similarity));
            return similarities;
        }

    }
}