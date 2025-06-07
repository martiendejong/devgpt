# Namenanalyse DevGPT.AgentFactory

Geef per bestandsnaam alle gevonden cryptische of onduidelijke identificatoren, de context (bestand, locatie binnen code), en doe een concreet voorstel voor hoe deze beter en zelfbeschrijvend kunnen worden genoemd.

## 1. AgentConfig (AgentConfig.cs)
- Name, Description, Prompt, Stores, Functions, CallsAgents, CallsFlows, ExplicitModify: Namen zijn duidelijk. Geen wijzigingen nodig.

## 2. AgentConfigFormatHelper (AgentConfigFormatHelper.cs)
- AutoDetectAndParse, IsLikelyJson: Namen zijn duidelijk.

## 3. AgentFactory (AgentFactory.cs)
### Slechte namen:
- storesConfig, agentsConfig, flowsConfig  (Voorstel: storesConfiguration, agentsConfiguration, flowsConfiguration)
- WriteMode (Voorstel: isWriteModeActive)
- Messages (Voorstel: chatMessages)
- OpenAiApiKey (Voorstel: openAiApiKey/openAIApiKey)
- bigQueryParameter, bigQueryDataSetParameter (Voorstel: parameterBigQuery, parameterBigQueryDataSet)
- private const string writeModeText (Voorstel: writeModeInstructionText)
- BigQuery_ExtractAsString, BigQuery_GetClient (Voorstel: ExtractBigQueryResultAsString, CreateBigQueryClient)
...
(Overige onderdelen zijn duidelijk of beschreven in de analyse.)

## 4. AgentManager (AgentManager.cs)
- _stores, _agents, _flows, etc. (Voorstel: zonder underscore tenzij private field)

## 5. BuildOutput (BuildOutput.cs)
- buildFile, buildOutputFile, buildErrorsFile (Voorstel: buildScriptFileName, buildOutputFileName, buildErrorLogFileName)

## ... etc ...

### Samenvatting hoofdaanbevelingen
- Geef alle variabelen, functies, methoden en klassen een volledig uitgespelde, zelfverklarende naam.
- Gebruik camelCase of PascalCase consistent volgens de geldende conventie binnen de codebase.
- Vermijd cryptische afkortingen of enkelletters tenzij algemeen geaccepteerd.
- Licht alle afwijkingen van de conventies toe in een korte comment.

**Rationale:**
Heldere identificatienamen versnellen het begrijpen, onderhouden en reviewen van code aanzienlijk. Door namen zo consequent en beschrijvend mogelijk te kiezen, verlaag je de cognitieve belasting en verklein je de kans op fouten of misinterpretatie bij doorontwikkeling.