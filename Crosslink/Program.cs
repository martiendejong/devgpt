using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Store;
using Store.Model;
using DevGPT.OpenAI;
using DevGPT.Classes;

class Program
{
    static async Task Main(string[] args)
    {
        // Load OpenAI settings using DevGPT libraries
        var openAISettings = Store.OpenAISettings.Load();
        string apikey = openAISettings.ApiKey;

        // Configure document stores for werkzoekenden and bedrijven using Store.DocumentStore and DocumentStoreConfig
        var werkzoekendenconfig = new DocumentStoreConfig(@"c:\stores\crosslink\werkzoekenden", @"c:\stores\crosslink\werkzoekenden.embed", apikey);
        var werkzoekenden = new DocumentStore(werkzoekendenconfig);

        var bedrijvenconfig = new DocumentStoreConfig(@"c:\stores\crosslink\bedrijven", @"c:\stores\crosslink\bedrijven.embed", apikey);
        var bedrijven = new DocumentStore(bedrijvenconfig);

        // Example CV data (this would be loaded dynamically in a production scenario)
        var candidateCV = @"**Curriculum Vitae**\n        \n**Persoonlijke gegevens**\nNaam: Mark van den Berg\nAdres: Keizersgracht 112, 1015 CV Amsterdam\nTelefoonnummer: 06-12345678\nE-mail: markvandenberg@email.com\nGeboortedatum: 12 mei 1990\nNationaliteit: Nederlands\nRijbewijs: B\n\n---\n**Profiel**\nAnalytische en gedreven IT-professional met een passie voor softwareontwikkeling en procesoptimalisatie. Ik ben een probleemoplosser die graag complexe vraagstukken analyseert en vertaalt naar efficiënte technologische oplossingen. Mijn sterke communicatieve vaardigheden maken mij een bruggenbouwer tussen techniek en eindgebruikers. ... (etc, rest of CV as per sample)";

        Console.WriteLine("Vacatures vinden op basis van het volgende CV:");
        Console.WriteLine(candidateCV);
        Console.WriteLine();

        // Find relevant jobs/vacancies using bedrijven store's interface
        List<string> relevantVacancies = await bedrijven.GetRelevantDocuments(candidateCV, new List<IStore>());

        int matchCount = 0;
        foreach (var vacature in relevantVacancies)
        {
            matchCount++;
            Console.WriteLine("BEGIN MATCH");
            Console.WriteLine("Match gevonden met vacature:");
            Console.WriteLine(vacature);
            Console.WriteLine();

            // Use DevGPT.OpenAI and DevGPT chat classes for interview/chat simulation
            var api = new OpenAIClient(apikey);
            var chatClient = new SimpleOpenAIClient(api, apikey, null);

            var messages = new List<ChatMessage>
            {
                new AssistantChatMessage("De CV van de kandidaat: " + candidateCV),
                new AssistantChatMessage("De vacature: " + vacature),
                new AssistantChatMessage("Genereer een sollicitatiegesprek tussen de kandidaat en de interviewer...")
            };
            string simulatedInterview = await chatClient.GetResponse(
                messages, 
                DevGPTChatResponseFormat.CreateTextFormat(), 
                null, 
                new List<ImageData>());

            // Analyse the match with DevGPT chat classes
            var analysisMessages = new List<ChatMessage>
            {
                new AssistantChatMessage("De CV van de kandidaat: " + candidateCV),
                new AssistantChatMessage("De vacature: " + vacature),
                new AssistantChatMessage("Het gesimuleerde gesprek: " + simulatedInterview),
                new AssistantChatMessage("Geef een voorlopige analyse conclusie beoordeling van de match en het gesimuleerde gesprek. Geef de match een score tussen 0 en 100...")
            };
            string analysis = await chatClient.GetResponse(
                analysisMessages, 
                DevGPTChatResponseFormat.CreateTextFormat(), 
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
