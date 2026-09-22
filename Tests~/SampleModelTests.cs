using System;
using System.IO;
using System.Threading.Tasks;
using JadedBelles.Util.SaveSystem;
using JadedBelles.Util.Samples.SaveSystemBasics;
using NUnit.Framework;

namespace JadedBelles.Util.SaveSystem.Tests
{
    // The importable sample is intentionally not a dependency of Unity package tests.
    // These extra checks compile and exercise its DTOs in the standalone harness.
    public sealed class SampleModelTests
    {
        [Test]
        public void SnapshotCopiesEveryMutableCollection()
        {
            var original = new SampleSaveData();
            original.Player.Inventory.Add(new ItemStack { ItemId = "potion", Quantity = 2 });
            original.Player.QuestProgress["intro"] = 1;
            original.World.Entities["a"] = new EntityState { EntityId = "a" };
            original.World.Entities["b"] = new EntityState { EntityId = "b" };
            original.World.Entities["a"].LinkedEntityIds.Add("b");
            var snapshot = original.CaptureSnapshot();
            original.Player.Inventory[0].Quantity = 5;
            original.Player.QuestProgress["intro"] = 9;
            original.Player.Position.X = 100;
            original.World.Entities["a"].Position.Y = 100;
            original.World.Entities["a"].LinkedEntityIds.Clear();
            Assert.That(snapshot.Player.Inventory[0].Quantity, Is.EqualTo(2));
            Assert.That(snapshot.Player.QuestProgress["intro"], Is.EqualTo(1));
            Assert.That(snapshot.Player.Position.X, Is.Zero);
            Assert.That(snapshot.World.Entities["a"].Position.Y, Is.Zero);
            Assert.That(snapshot.World.Entities["a"].LinkedEntityIds, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task SampleModelsRoundTripAndRejectDanglingLinks()
        {
            string directory = Path.Combine(Path.GetTempPath(), "jb-save-sample-" + Guid.NewGuid().ToString("N"));
            try
            {
                var manager = new SaveManager<SampleSaveData>(directory, "slot-1", SampleSaveData.CreateDefinition());
                var result = await manager.LoadAsync();
                result.Data.World.Entities["a"] = new EntityState { EntityId = "a" };
                result.Data.World.Entities["a"].LinkedEntityIds.Add("a");
                await manager.SaveAsync(result.Data.CaptureSnapshot());
                Assert.That((await manager.LoadAsync()).Data.World.Entities["a"].LinkedEntityIds[0], Is.EqualTo("a"));
                result.Data.World.Entities["a"].LinkedEntityIds.Add("missing");
                Assert.ThrowsAsync<InvalidSaveException>(async () => await manager.SaveAsync(result.Data.CaptureSnapshot()));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void SampleRejectsNonFinitePosition()
        {
            var data = new SampleSaveData();
            data.Player.Position.X = float.NaN;
            Assert.Throws<InvalidSaveException>(() => SampleSaveData.CreateDefinition().ValidateData(data));
        }
    }
}
