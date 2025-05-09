using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Store;
using Store.Model;
using Store.Helpers;
using DevGPT.Classes;
using DevGPT.OpenAI;

class Program
{
    static async Task Main(string[] args)
    {
        // Load OpenAI settings using DevGPT libraries
        var openAISettings = Store.OpenAISettings.Load();
        string apikey = openAISettings.ApiKey;

        // Configure document stores for werkzoekenden and bedrijven
        var werkzoekendenconfig = new DocumentStoreConfig(@"c:\stores\crosslink\werkzoekenden", @"c:\stores\crosslink\werkzoekenden.embed", apikey);
        var werkzoekenden = new Store.DocumentStore(werkzoekendenconfig);

        var bedrijvenconfig = new DocumentStoreConfig(@"c:\stores\crosslink\bedrijven", @"c:\stores\crosslink\bedrijven.embed", apikey);
        var bedrijven = new Store.DocumentStore(bedrijvenconfig);

        // Example CV data (normally loaded from werkzoekenden DB)
        var data = @"**Curriculum Vitae**  \n\n**Persoonlijke gegevens**  \nNaam: Mark van den Berg  \nAdres: Keizersgracht 112, 1015 CV Amsterdam  \nTelefoonnummer: 06-12345678  \nE-mail: markvandenberg@email.com  \nGeboortedatum: 12 mei 1990  \nNationaliteit: Nederlands  \nRijbewijs: B  \n\n---\n\n**Profiel**  \nAnalytische en gedreven IT-professional met een passie voor softwareontwikkeling en procesoptimalisatie. Ik ben een probleemoplosser die graag complexe vraagstukken analyseert en vertaalt naar efficiënte technologische oplossingen. Mijn sterke communicatieve vaardigheden maken mij een bruggenbouwer tussen techniek en eindgebruikers. ... (etc, rest of CV as per sample)";

        Console.WriteLine("Vacatures vinden op basis van het volgende CV:");
        Console.WriteLine(data);
        Console.WriteLine();

        // Vind relevante vacatures via DevGPT store interface
        var matches = new List<Tuple<string, string>>();
        var relevant = await bedrijven.GetRelevantDocuments(data, new List<IStore>());
        foreach (var vacancy in relevant)
        {
            matches.Add(new Tuple<string, string>(data, vacancy));
        }

        // Set up OpenAI client for interview/chat simulation
        var api = new OpenAIClient(apikey);
        var chatClient = new SimpleOpenAIClient(api, apikey, (List<DevGPT.Classes.ChatMessage> _msgs, string _responseContent) => { });
        int i = 0;
        foreach (var match in matches)
        {
            ++i;
            Console.WriteLine("BEGIN MATCH");
            Console.WriteLine("Match gevonden met vacature:");
            Console.WriteLine(match.Item2);
            Console.WriteLine();

            // Simuleer een gesprek
            var messages = new List<DevGPT.Classes.ChatMessage>
            {
                new AssistantChatMessage("De CV van de kandidaat: " + match.Item1),
                new AssistantChatMessage("De vacature: " + match.Item2),
                new AssistantChatMessage("Genereer een sollicitatiegesprek tussen de kandidaat en de interviewer...")
            };
            var simulatedInterview = await chatClient.GetResponse(messages, DevGPT.Classes.DevGPTChatResponseFormat.CreateTextFormat(), null, new List<DevGPT.Classes.ImageData>());

            // Analyseer de match
            messages = new List<DevGPT.Classes.ChatMessage>
            {
                new AssistantChatMessage("De CV van de kandidaat: " + match.Item1),
                new AssistantChatMessage("De vacature: " + match.Item2),
                new AssistantChatMessage("Het gesimuleerde gesprek: " + simulatedInterview),
                new AssistantChatMessage("Geef een voorlopige analyse conclusie beoordeling van de match en het gesimuleerde gesprek. Geef de match een score tussen 0 en 100...")
            };
            var analysis = await chatClient.GetResponse(messages, DevGPT.Classes.DevGPTChatResponseFormat.CreateTextFormat(), null, new List<DevGPT.Classes.ImageData>());

            var report = $@"Er is een match gevonden met een CV en een vacature.\n\nDe CV:\n{match.Item1}\n\nDe vacature:\n{match.Item2}\n\nOp basis van de match van CV en vacature wordt het volgende gesprek gesimuleerd:\n{simulatedInterview}\n\nHier volgt de analyse van de match tot zover.\n{analysis}\n";

            File.WriteAllText($"c:\\stores\\crosslink\\Match {i}.txt", report);

            Console.WriteLine("Op basis van de match van CV en vacature wordt het volgende gesprek gesimuleerd:");
            Console.WriteLine(simulatedInterview);
            Console.WriteLine("EINDE MATCH");
            Console.WriteLine();
            Console.WriteLine("Druk op enter om verder te gaan.");
            Console.ReadLine();
        }
    }
}
