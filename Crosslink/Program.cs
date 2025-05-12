using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Load OpenAI settings using DevGPT libraries
        var openAIConfig = OpenAIConfig.Load();
        string apikey = openAIConfig.ApiKey;
        var llmClient = new OpenAIClientWrapper(openAIConfig);

        // Prepare document stores for werkzoekenden and bedrijven with your in-repo DocumentStore components
        var werkzoekendenEmbeddingStore = new EmbeddingFileStore(@"c:\stores\crosslink\werkzoekenden.embed", llmClient);
        var werkzoekendenTextStore = new TextFileStore(@"c:\stores\crosslink\werkzoekenden");
        var werkzoekendenPartStore = new DocumentPartFileStore(@"c:\stores\crosslink\werkzoekenden.parts");
        var werkzoekenden = new DocumentStore(werkzoekendenEmbeddingStore, werkzoekendenTextStore, werkzoekendenPartStore, llmClient);

        var bedrijvenEmbeddingStore = new EmbeddingFileStore(@"c:\stores\crosslink\bedrijven.embed", llmClient);
        var bedrijvenTextStore = new TextFileStore(@"c:\stores\crosslink\bedrijven");
        var bedrijvenPartStore = new DocumentPartFileStore(@"c:\stores\crosslink\bedrijven.parts");
        var bedrijven = new DocumentStore(bedrijvenEmbeddingStore, bedrijvenTextStore, bedrijvenPartStore, llmClient);

        // Example CV data (this would be loaded dynamically in a production scenario)
        var candidateCV = @"**Curriculum Vitae**\n        \n**Persoonlijke gegevens**\nNaam: Mark van den Berg\nAdres: Keizersgracht 112, 1015 CV Amsterdam\nTelefoonnummer: 06-12345678\nE-mail: markvandenberg@email.com\nGeboortedatum: 12 mei 1990\nNationaliteit: Nederlands\nRijbewijs: B\n\n---\n**Profiel**\nAnalytische en gedreven IT-professional met een passie voor softwareontwikkeling en procesoptimalisatie. Ik ben een probleemoplosser die graag complexe vraagstukken analyseert en vertaalt naar efficiënte technologische oplossingen. Mijn sterke communicatieve vaardigheden maken mij een bruggenbouwer tussen techniek en eindgebruikers. ... (etc, rest of CV as per sample)";

        Console.WriteLine("Vacatures vinden op basis van het volgende CV:");
        Console.WriteLine(candidateCV);
        Console.WriteLine();

        // Find relevant jobs/vacancies using bedrijven store's interface, using RelevantItems
        List<string> relevantVacancies = await bedrijven.RelevantItems(candidateCV);

        int matchCount = 0;
        foreach (var vacature in relevantVacancies)
        {
            matchCount++;
            Console.WriteLine("BEGIN MATCH");
            Console.WriteLine("Match gevonden met vacature:");
            Console.WriteLine(vacature);
            Console.WriteLine();

            // Use DevGPT.OpenAI and DevGPT chat classes for interview/chat simulation
            var chatClient = llmClient;
            var messages = new List<DevGPTChatMessage>
            {
                new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = "De CV van de kandidaat: " + candidateCV },
                new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = "De vacature: " + vacature },
                new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = "Genereer een sollicitatiegesprek tussen de kandidaat en de interviewer..." }
            };
            string simulatedInterview = await chatClient.GetResponse(
                messages, 
                DevGPTChatResponseFormat.Text, 
                null, 
                new List<ImageData>());

            // Analyse the match
            var analysisMessages = new List<DevGPTChatMessage>
            {
                new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = "De CV van de kandidaat: " + candidateCV },
                new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = "De vacature: " + vacature },
                new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = "Het gesimuleerde gesprek: " + simulatedInterview },
                new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = "Geef een voorlopige analyse conclusie beoordeling van de match en het gesimuleerde gesprek. Geef de match een score tussen 0 en 100..." }
            };
            string analysis = await chatClient.GetResponse(
                analysisMessages, 
                DevGPTChatResponseFormat.Text, 
                null, 
                new List<ImageData>());

            string report = $@"Er is een match gevonden met een CV en een vacature.\n\nDe CV:\n{candidateCV}\n\nDe vacature:\n{vacature}\n\nOp basis van de match van CV en vacature wordt het volgende gesprek gesimuleerd:\n{simulatedInterview}\n\nHier volgt de analyse van de match tot zover.\n{analysis}\n";

            File.WriteAllText($@"c:\\stores\\crosslink\\Match {matchCount}.txt", report);

            Console.WriteLine("Op basis van de match van CV en vacature wordt het volgende gesprek gesimuleerd:");
            Console.WriteLine(simulatedInterview);
            Console.WriteLine("EINDE MATCH");
            Console.WriteLine();
            Console.WriteLine("Druk op enter om verder te gaan.");
            Console.ReadLine();
        }
    }
}
