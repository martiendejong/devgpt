// See https://aka.ms/new-console-template for more information
using DevGPT.NewAPI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Load API settings
        var openAISettings = OpenAIConfig.Load();
        string apikey = openAISettings.ApiKey;

        // Configure document stores using shared library
        var werkzoekendenconfig = new DocumentStoreConfig(@"c:\stores\crosslink\werkzoekenden", @"c:\stores\crosslink\werkzoekenden.embed", apikey);
        var werkzoekenden = new DocumentStore(werkzoekendenconfig);

        var bedrijvenconfig = new DocumentStoreConfig(@"c:\stores\crosslink\bedrijven", @"c:\stores\crosslink\bedrijven.embed", apikey);
        var bedrijven = new DocumentStore(bedrijvenconfig);

        // Example candidate CV data (would normally come from a document in werkzoekenden)
        var data = @"**Curriculum Vitae**  

**Persoonlijke gegevens**  
Naam: Mark van den Berg  
Adres: Keizersgracht 112, 1015 CV Amsterdam  
Telefoonnummer: 06-12345678  
E-mail: markvandenberg@email.com  
Geboortedatum: 12 mei 1990  
Nationaliteit: Nederlands  
Rijbewijs: B  

---

**Profiel**  
Analytische en gedreven IT-professional met een passie voor softwareontwikkeling en procesoptimalisatie. Ik ben een probleemoplosser die graag complexe vraagstukken analyseert en vertaalt naar efficiënte technologische oplossingen. Mijn sterke communicatieve vaardigheden maken mij een bruggenbouwer tussen techniek en eindgebruikers.

---

**Opleiding**  
2008 - 2012  
BSc Informatica, Universiteit van Amsterdam  
Afstudeerrichting: Softwareontwikkeling

2002 - 2008  
VWO, Stedelijk Gymnasium Utrecht  
Profiel: Natuur & Techniek

---

**Werkervaring**  

**2020 - heden**  
*Software Engineer | TechSolutions BV, Amsterdam*  
- Ontwikkelen en optimaliseren van cloudgebaseerde applicaties in Python en JavaScript.  
- Implementeren van CI/CD-pipelines en automatiseren van testprocessen.  
- Samenwerken met UX-designers en product owners om gebruiksvriendelijke interfaces te realiseren.  

**2016 - 2020**  
*IT Consultant | DataTech Consultancy, Utrecht*  
- Adviseren van klanten over digitale transformatie en procesautomatisering.  
- Begeleiden van software-implementaties en geven van trainingen aan eindgebruikers.  
- Ontwikkelen van maatwerkoplossingen in samenwerking met ontwikkelteams.  

**2012 - 2016**  
*Junior Developer | WebWare Solutions, Rotterdam*  
- Ontwerpen en ontwikkelen van webapplicaties met PHP en JavaScript.  
- Optimaliseren van databases en backend-systemen voor betere prestaties.  
- Ondersteunen van klanten bij technische problemen en onderhoud van applicaties.  

---

**Vaardigheden**  
- Programmeertalen: Python, JavaScript, TypeScript, PHP  
- Frameworks: React, Node.js, Django  
- Cloudplatformen: AWS, Azure  
- DevOps: CI/CD, Docker, Kubernetes  
- Databases: PostgreSQL, MongoDB, MySQL  
- Soft skills: Probleemoplossend vermogen, communicatief sterk, teamspeler  

---

**Certificeringen**  
- AWS Certified Solutions Architect (2021)  
- Microsoft Certified: Azure Fundamentals (2020)  
- Scrum Master Certified (2019)  

---

**Talen**  
- Nederlands: Moedertaal  
- Engels: Vloeiend  
- Duits: Basisniveau  

---

**Hobby’s & interesses**  
- Programmeren van hobbyprojecten en open-source bijdragen  
- Schaken en strategische bordspellen  
- Hardlopen en fitness  
- Reizen en nieuwe culturen ontdekken  

---

**Voorbeeldgesprek met een recruiter**  

**Recruiter:** Goedemiddag Mark, fijn dat je er bent. Kun je iets over jezelf vertellen?  
**Mark:** Zeker! Ik ben een gepassioneerde softwareontwikkelaar met ervaring in cloudgebaseerde oplossingen en procesoptimalisatie. Ik werk graag in een omgeving waar ik mijn analytische en probleemoplossende vaardigheden kan inzetten om complexe vraagstukken op te lossen. Daarnaast hou ik van samenwerken met multidisciplinaire teams om gebruiksvriendelijke en efficiënte softwareoplossingen te ontwikkelen.  

**Recruiter:** Dat klinkt goed! Wat motiveert jou in je werk?  
**Mark:** Ik haal veel voldoening uit het oplossen van technische uitdagingen en het verbeteren van processen. Het gevoel dat ik een impact kan maken door technologie in te zetten om efficiëntere en slimmere oplossingen te creëren, is voor mij heel belangrijk. Daarnaast vind ik het leuk om te blijven leren en nieuwe technologieën te verkennen.  

**Recruiter:** Kun je een voorbeeld geven van een project waar je trots op bent?  
**Mark:** Zeker! Bij TechSolutions heb ik gewerkt aan een CI/CD-implementatie die de software-releasecyclus met 40% heeft versneld. Dit project vereiste niet alleen technische kennis, maar ook samenwerking met verschillende teams om de implementatie soepel te laten verlopen. Het was geweldig om te zien hoeveel efficiënter het ontwikkelproces daarna werd.  

**Recruiter:** Interessant! Wat zoek je in een nieuwe uitdaging?  
**Mark:** Ik zoek een omgeving waar innovatie en samenwerking centraal staan. Een plek waar ik kan blijven groeien en waar ik de ruimte krijg om impactvolle softwareoplossingen te ontwikkelen. Het liefst werk ik in een dynamisch team dat zich richt op moderne technologieën en best practices.  

**Recruiter:** Dat klinkt als een goede match met onze organisatie. We nemen binnenkort contact met je op!  
**Mark:** Dank je wel! Ik kijk uit naar jullie reactie.  

---

**Referenties op aanvraag beschikbaar.**";

        Console.WriteLine("Vacatures vinden op basis van het volgende CV:");
        Console.WriteLine(data);
        Console.WriteLine();

        // Find relevant vacancies using shared library call
        var matches = new List<Tuple<string, string>>();
        var relevant = await bedrijven.GetRelevantDocuments(data, new List<IStore>());
        foreach (var item1 in relevant)
        {
            matches.Add(new Tuple<string, string>(data, item1));
        }

        var api = new OpenAI.OpenAIClient(apikey);
        var client = new SimpleOpenAIClient(api, apikey, (List<ChatMessage> messages, string responseContent) => { });
        var i = 0;
        foreach (var match in matches)
        {
            ++i;
            Console.WriteLine("BEGIN MATCH");
            Console.WriteLine("Er is een match gevonden met de volgende vacature:");
            Console.WriteLine(match.Item2);
            Console.WriteLine();

            var messages = new List<ChatMessage>();
            messages.Add(new AssistantChatMessage("De CV van de kandiaat: " + match.Item1));
            messages.Add(new AssistantChatMessage("De vacature: " + match.Item2));
            messages.Add(new AssistantChatMessage("Genereer een sollicitatiegesprek tussen de kandidaat en de interviewer op basis van het cv van de kandidaat, de vacature, en de voorbeeldgesprekken met de recruiter. In dit gesprek worden inhoudelijke vragen gesteld over kennis die nodig is voor het uitvoeren van de functie. De sollicitant vraagt op diens beurt of de opgedane kennis bij deze vacature van belang zijn. Gebruik de informatie die is bijgevoegd in het gesprek."));
            var response = await client.GetResponse(messages, ChatResponseFormat.CreateTextFormat(), null, new List<ImageData>());

            messages = new List<ChatMessage>();
            messages.Add(new AssistantChatMessage("De CV van de kandiaat: " + match.Item1));
            messages.Add(new AssistantChatMessage("De vacature: " + match.Item2));
            messages.Add(new AssistantChatMessage("Het gesimuleerde gesprek: " + response));
            messages.Add(new AssistantChatMessage("Geef een voorlopige analyse conclusie beoordeling van de natch en het gesimuleerde gesprek. Geef de match een score tussen 0 en 100 waar 100 een volledige match is en 0 totaal geen match. Beschrijf de match in 500 woorden en geef aan op welke punten er een sterke match is en welke punten juist aangeven dat er misschien geen match is. Geef aan het eind in één zin aan of je deze sollicitant wel of niet in dienst zou nemen en waarom. Geef ook in één zin aan of je deze baan zou aannemen en waarom."));
            var rating = await client.GetResponse(messages, ChatResponseFormat.CreateTextFormat(), null, new List<ImageData>());

            var finalText = $@"Er is een match gevonden met een CV en een vacature.

De CV:
{match.Item1}

De vacature:
{match.Item2}

Op basis van de match van CV en vacature wordt het volgende gesprek gesimuleerd:
{response}

Hier volgt de analyse van de match tot zover.
{rating}
";
            File.WriteAllText(@$"c:\stores\crosslink\Match {i}.txt", finalText);

            Console.WriteLine("Op basis van de match van CV en vacature wordt het volgende gesprek gesimuleerd:");
            Console.WriteLine(response);
            Console.WriteLine("EINDE MATCH");
            Console.WriteLine();

            Console.WriteLine("Druk op enter om verder te gaan.");
        }
    }
}
