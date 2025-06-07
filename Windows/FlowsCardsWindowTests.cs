using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DevGPT;

namespace DevGPT.Tests
{
    /// <summary>
    /// Tests voor herschikken van agents in CallsAgents binnen een ObservableCollection via de drag/drop-functionaliteit
    /// zoals gebruikt in FlowsCardsWindow.
    /// </summary>
    [TestClass]
    public class FlowsCardsWindowTests
    {
        /// <summary>
        /// Test of een agent juist van index A naar B wordt geplaatst in de CallsAgents-ObservableCollection.
        /// Dit simuleert de drag/drop-functionaliteit.
        /// </summary>
        [TestMethod]
        public void Test_CallsAgents_Reorder_MoveItemToNewIndex()
        {
            // Arrange
            var card = new FlowCardModel
            {
                Name = "ExampleFlow",
                Description = "Test flow",
                CallsAgents = new ObservableCollection<string> { "Alpha", "Bravo", "Charlie", "Delta" }
            };
            // Pre-check: beginsituatie
            CollectionAssert.AreEqual(new[] { "Alpha", "Bravo", "Charlie", "Delta" }, card.CallsAgents.ToList(), "Initial order incorrect");

            // Act: verplaats "Bravo" (index 1) naar index 3 (achter Delta)
            int oldIndex = 1;
            int newIndex = 3;
            card.CallsAgents.Move(oldIndex, newIndex);

            // Assert: verwacht ["Alpha", "Charlie", "Delta", "Bravo"]
            CollectionAssert.AreEqual(new[] { "Alpha", "Charlie", "Delta", "Bravo" }, card.CallsAgents.ToList(), "Agent order was not updated correctly after reorder");
        }

        /// <summary>
        /// (Optioneel) Test: simuleert opslaan van een flow, en controleert of de volgorde mee weggeschreven wordt.
        /// </summary>
        [TestMethod]
        public void Test_SaveFlowConfig_PreservesAgentOrder()
        {
            // Arrange
            var card = new FlowCardModel
            {
                Name = "SaveTest",
                Description = "Opslaantest",
                CallsAgents = new ObservableCollection<string> { "A1", "A2", "A3" }
            };
            // Wijzig volgorde: verplaats A2 naar het einde
            card.CallsAgents.Move(1, 2);

            // Act: opslaan als FlowConfig
            var flowConfig = new FlowConfig {
                Name = card.Name,
                Description = card.Description,
                CallsAgents = card.CallsAgents.ToList()
            };

            // Assert: volgorde behouden?
            CollectionAssert.AreEqual(new[] { "A1", "A3", "A2" }, flowConfig.CallsAgents, "Opslaan verliest aangepaste volgoorde");
        }
    }
}
