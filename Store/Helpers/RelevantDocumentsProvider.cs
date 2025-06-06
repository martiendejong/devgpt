﻿using OpenAI.Chat;
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

        public async Task<string> GetRelevantDocumentsAsString(string query, List<Embedding> embeddings, List<IStore> otherStores)
        {
            List<string> selectedDocuments = await GetRelevantDocuments(query, embeddings, otherStores);

            // Return document names as a comma-separated string
            //return string.Join(", ", selectedDocuments.Select(d => d.Name));
            return string.Join("\n\n", selectedDocuments);
        }

        public async Task<List<ChatMessage>> GetRelevantDocumentsAsChatMessages(string query, List<Embedding> embeddings, List<IStore> otherStores)
        {
            List<string> selectedDocuments = await GetRelevantDocuments(query, embeddings, otherStores);
            return selectedDocuments.Select(d => new AssistantChatMessage(d) as ChatMessage).ToList();
        }

        public class MultiStoreEmbeddings
        {
            public RelevantDocumentsProvider Provider;
            public double Similarity;
            public Embedding Document;
        }

        public async Task<List<string>> GetRelevantDocuments(string query, List<Embedding> embeddings, List<IStore> otherStores)
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
            var total = documents.Select(d => new MultiStoreEmbeddings { Provider = this, Similarity = d.Item1, Document = d.Item2 }).ToList();

            foreach (var o in otherStores) {
                var odocuments = await o.RelevantDocumentsProvider.GetDocumentsWithSimilarity(query, o.GetEmbeddings());
                total.AddRange(odocuments.Select(d => new MultiStoreEmbeddings { Provider = o.RelevantDocumentsProvider, Similarity = d.Item1, Document = d.Item2 }).ToList());
            }
            total = total.OrderByDescending(t => t.Similarity).ToList();

            // List to store selected documents
            var selectedDocuments = new List<string>();
            int currentTokenCount = 0;

            foreach (var document in total)
            {
                var path = document.Provider.PathProvider.GetPath(document.Document.Path);
                var text = File.ReadAllText(path);

                // Count tokens for the current document
                int documentTokenCount = TokenCounter.CountTokens(text);

                // Check if adding this document would exceed max tokens
                if (currentTokenCount + documentTokenCount <= maxTokens)
                {
                    if(document.Provider == this)
                    {
                        var documentView = $"File name: {document.Document.Name}\nFile path: {document.Document.Path}\nFile content:\n{text}";
                        selectedDocuments.Add(documentView);
                    }
                    else
                    {
                        var documentView = $"{document.Document.Name}:\n{text}";
                        selectedDocuments.Add(documentView);
                    }
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