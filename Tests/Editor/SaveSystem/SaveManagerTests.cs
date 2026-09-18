using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace JadedBelles.Util.SaveSystem.Tests
{
    public sealed class SaveManagerTests
    {
        private string _directory;
        private string Primary => Path.Combine(_directory, "slot-1.json");
        private string Backup => Primary + ".bak";

        [SetUp]
        public void SetUp() { _directory = Path.Combine(Path.GetTempPath(), "jb-save-tests-" + Guid.NewGuid().ToString("N")); }

        [TearDown]
        public void TearDown() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }

        [JsonObject(MemberSerialization.OptIn)]
        public sealed class Progress
        {
            [JsonProperty(Required = Required.Always)] public long Coins;
            [JsonProperty(Required = Required.Always)] public Dictionary<string, List<string>> Links =
                new Dictionary<string, List<string>>();
            [JsonIgnore] public Progress RuntimeReference;
        }

        [JsonObject(MemberSerialization.OptIn)]
        public sealed class Preferences
        {
            [JsonProperty(Required = Required.Always)] public bool MusicEnabled = true;
        }

        [JsonObject(MemberSerialization.OptIn)]
        public sealed class RecursiveData
        {
            [JsonProperty] public RecursiveData Child;
        }

        private static SaveDefinition<Progress> Definition(int bytes = 1024 * 1024,
            Action<Progress> validate = null, IDictionary<int, Action<JObject>> migrations = null)
        {
            return new SaveDefinition<Progress>("test.progress", 2, () => new Progress(),
                validate ?? (data =>
                {
                    if (data.Coins < 0 || data.Links == null) throw new InvalidSaveException("Invalid progress.");
                }), migrations ?? new Dictionary<int, Action<JObject>>
                {
                    [1] = data =>
                    {
                        if (data["Currency"]?.Type != JTokenType.Integer) throw new InvalidSaveException("Invalid v1.");
                        data["Coins"] = data["Currency"].DeepClone();
                        data.Remove("Currency");
                        data["Links"] = new JObject();
                    }
                }, bytes);
        }

        private SaveManager<Progress> Manager(Action<SaveWriteStage> checkpoint = null,
            SaveDefinition<Progress> definition = null)
            => new SaveManager<Progress>(_directory, "slot-1", definition ?? Definition(), checkpoint);

        private async Task<SaveManager<Progress>> Seed()
        {
            var manager = Manager();
            await manager.LoadAsync();
            await manager.SaveAsync(new Progress { Coins = 10 });
            await manager.SaveAsync(new Progress { Coins = 20 });
            return manager;
        }

        private void Edit(Action<JObject> change)
        {
            var root = JObject.Parse(File.ReadAllText(Primary));
            change(root);
            File.WriteAllText(Primary, root.ToString());
        }

        [Test]
        public async Task MissingFilesReturnValidatedDefaults()
        {
            var result = await Manager().LoadAsync();
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.NewGame));
            Assert.That(result.Data.Coins, Is.Zero);
            Assert.That(File.Exists(Primary), Is.False);
        }

        [Test]
        public async Task DictionaryAndLogicalCyclesRoundTripWithoutRuntimeReference()
        {
            var manager = Manager();
            await manager.LoadAsync();
            var data = new Progress { Coins = 123 };
            data.Links["a"] = new List<string> { "b" };
            data.Links["b"] = new List<string> { "a" };
            data.RuntimeReference = data;
            await manager.SaveAsync(data);
            var result = await manager.LoadAsync();
            Assert.That(result.Data.Coins, Is.EqualTo(123));
            Assert.That(result.Data.Links["b"][0], Is.EqualTo("a"));
            Assert.That(result.Data.RuntimeReference, Is.Null);
        }

        [Test]
        public async Task SupportsDifferentGameModels()
        {
            var definition = new SaveDefinition<Preferences>("test.preferences", 1,
                () => new Preferences(), data => { });
            var manager = new SaveManager<Preferences>(_directory, "options", definition);
            await manager.LoadAsync();
            await manager.SaveAsync(new Preferences { MusicEnabled = false });
            Assert.That((await manager.LoadAsync()).Data.MusicEnabled, Is.False);
        }

        [Test]
        public void SaveBeforeSuccessfulLoadIsBlocked()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Manager().SaveAsync(new Progress()));
        }

        [Test]
        public async Task CorruptPrimaryRecoversAndPreservesEvidence()
        {
            var manager = await Seed();
            File.WriteAllText(Primary, "{broken");
            var result = await manager.LoadAsync();
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.RecoveredBackup));
            Assert.That(result.Data.Coins, Is.EqualTo(10));
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            string backup = File.ReadAllText(Backup);
            await manager.SaveAsync(result.Data);
            Assert.That(File.ReadAllText(Backup), Is.EqualTo(backup));
            Assert.That(Directory.GetFiles(_directory, "*.corrupt-*").Length, Is.EqualTo(1));
        }

        [Test]
        public async Task BothCorruptBlockWritesWithoutReset()
        {
            var manager = await Seed();
            File.WriteAllText(Primary, "{bad");
            File.WriteAllText(Backup, "{bad");
            var result = await manager.LoadAsync();
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Failed));
            Assert.That(result.Data, Is.Null);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await manager.SaveAsync(new Progress()));
            Assert.That(File.ReadAllText(Primary), Is.EqualTo("{bad"));
        }

        [Test]
        public async Task MissingPrimaryRecoversBackup()
        {
            var manager = await Seed();
            File.Delete(Primary);
            Assert.That((await manager.LoadAsync()).Status, Is.EqualTo(SaveLoadStatus.RecoveredBackup));
        }

        [Test]
        public async Task MissingBackupDoesNotTurnCorruptionIntoNewGame()
        {
            var manager = await Seed();
            File.Delete(Backup);
            File.WriteAllText(Primary, "{bad");
            Assert.That((await manager.LoadAsync()).Status, Is.EqualTo(SaveLoadStatus.Failed));
        }

        [TestCase("future")]
        [TestCase("schema")]
        [TestCase("format")]
        public async Task IncompatibleSaveNeverFallsBack(string kind)
        {
            var manager = await Seed();
            Edit(root =>
            {
                if (kind == "future") root["schemaVersion"] = 99;
                if (kind == "schema") root["schemaId"] = "another.game";
                if (kind == "format") root["format"] = "another.format";
            });
            string before = File.ReadAllText(Primary);
            var result = await manager.LoadAsync();
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Failed));
            Assert.That(result.Errors[0], Is.TypeOf<SaveCompatibilityException>());
            Assert.ThrowsAsync<InvalidOperationException>(async () => await manager.SaveAsync(new Progress()));
            Assert.That(File.ReadAllText(Primary), Is.EqualTo(before));
        }

        [Test]
        public async Task MigrationChangesMemoryBeforeAnExplicitSave()
        {
            var manager = await Seed();
            Edit(root =>
            {
                root["schemaVersion"] = 1;
                var data = (JObject)root["data"];
                data["Currency"] = data["Coins"].DeepClone();
                data.Remove("Coins");
                data.Remove("Links");
            });
            var result = await manager.LoadAsync();
            Assert.That(result.Data.Coins, Is.EqualTo(20));
            Assert.That(JObject.Parse(File.ReadAllText(Primary))["schemaVersion"].Value<int>(), Is.EqualTo(1));
            await manager.SaveAsync(result.Data);
            Assert.That(JObject.Parse(File.ReadAllText(Primary))["schemaVersion"].Value<int>(), Is.EqualTo(2));
        }

        [Test]
        public async Task MissingMigrationIsIncompatibleNotCorrupt()
        {
            await Seed();
            Edit(root => root["schemaVersion"] = 1);
            var result = await Manager(definition: Definition(migrations: new Dictionary<int, Action<JObject>>())).LoadAsync();
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Failed));
            Assert.That(result.Errors[0], Is.TypeOf<SaveCompatibilityException>());
        }

        [Test]
        public async Task RequiredFieldsAreNotSilentlyDefaulted()
        {
            var manager = await Seed();
            Edit(root => ((JObject)root["data"]).Remove("Coins"));
            Assert.That((await manager.LoadAsync()).Status, Is.EqualTo(SaveLoadStatus.RecoveredBackup));
        }

        [Test]
        public async Task ExcessiveDepthIsRejected()
        {
            var manager = await Seed();
            File.WriteAllText(Primary, "{\"x\":" + new string('[', 100) + "0" + new string(']', 100) + "}");
            Assert.That((await manager.LoadAsync()).Status, Is.EqualTo(SaveLoadStatus.RecoveredBackup));
        }

        [Test]
        public async Task InvalidUtf8IsRejected()
        {
            var manager = await Seed();
            File.WriteAllBytes(Primary, new byte[] { 0xff, 0xfe, 0xff });
            Assert.That((await manager.LoadAsync()).Status, Is.EqualTo(SaveLoadStatus.RecoveredBackup));
        }

        [Test]
        public async Task OversizedInputIsRejected()
        {
            var manager = await Seed();
            using (var stream = new FileStream(Primary, FileMode.Create)) stream.SetLength(1024 * 1024 + 1);
            Assert.That((await manager.LoadAsync()).Status, Is.EqualTo(SaveLoadStatus.RecoveredBackup));
        }

        [Test]
        public async Task OversizedOutputCannotReplaceCommittedData()
        {
            await Seed();
            var manager = Manager(definition: Definition(bytes: 1024));
            await manager.LoadAsync();
            string before = File.ReadAllText(Primary);
            var data = new Progress();
            data.Links[new string('x', 2048)] = new List<string>();
            Assert.ThrowsAsync<InvalidSaveException>(async () => await manager.SaveAsync(data));
            Assert.That(File.ReadAllText(Primary), Is.EqualTo(before));
        }

        [Test]
        public async Task SemanticValidationRejectsBadSnapshot()
        {
            var manager = await Seed();
            string before = File.ReadAllText(Primary);
            Assert.ThrowsAsync<InvalidSaveException>(async () => await manager.SaveAsync(new Progress { Coins = -1 }));
            Assert.That(File.ReadAllText(Primary), Is.EqualTo(before));
        }

        [Test]
        public async Task CallbackProgrammingErrorsPropagateWithoutRollback()
        {
            await Seed();
            var manager = Manager(definition: Definition(validate: data => throw new NullReferenceException("Bug")));
            Assert.ThrowsAsync<NullReferenceException>(async () => await manager.LoadAsync());
            Assert.ThrowsAsync<InvalidOperationException>(async () => await manager.SaveAsync(new Progress()));
        }

        [Test]
        public async Task PreCancelledSavePreservesData()
        {
            var manager = await Seed();
            string before = File.ReadAllText(Primary);
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                Assert.CatchAsync<OperationCanceledException>(async () =>
                    await manager.SaveAsync(new Progress(), cancellation.Token));
            }
            Assert.That(File.ReadAllText(Primary), Is.EqualTo(before));
        }

        [TestCase("TemporaryFlushed")]
        [TestCase("BackupPrepared")]
        public async Task StorageFailureAtCommitBoundaryPreservesPrimary(string failAtName)
        {
            var failAt = (SaveWriteStage)Enum.Parse(typeof(SaveWriteStage), failAtName);
            await Seed();
            string before = File.ReadAllText(Primary);
            var manager = Manager(stage => { if (stage == failAt) throw new IOException("Injected storage failure"); });
            await manager.LoadAsync();
            Assert.ThrowsAsync<IOException>(async () => await manager.SaveAsync(new Progress()));
            Assert.That(File.ReadAllText(Primary), Is.EqualTo(before));
            Assert.That(Directory.GetFiles(_directory, "*.tmp*"), Is.Empty);
            if (failAt == SaveWriteStage.BackupPrepared) Assert.That(File.ReadAllText(Backup), Is.EqualTo(before));
        }

        [Test]
        public async Task CancellationAtCommitBoundaryPreservesPrimary()
        {
            await Seed();
            string before = File.ReadAllText(Primary);
            using (var cancellation = new CancellationTokenSource())
            {
                var manager = Manager(stage =>
                {
                    if (stage == SaveWriteStage.BackupPrepared) cancellation.Cancel();
                });
                await manager.LoadAsync();
                Assert.CatchAsync<OperationCanceledException>(async () =>
                    await manager.SaveAsync(new Progress(), cancellation.Token));
            }
            Assert.That(File.ReadAllText(Primary), Is.EqualTo(before));
            Assert.That(Directory.GetFiles(_directory, "*.tmp*"), Is.Empty);
        }

        [Test]
        public async Task FailedFirstSaveDoesNotPublishIncompleteFile()
        {
            var manager = Manager(stage => { throw new IOException("Injected storage failure"); });
            await manager.LoadAsync();
            Assert.ThrowsAsync<IOException>(async () => await manager.SaveAsync(new Progress()));
            Assert.That(File.Exists(Primary), Is.False);
            Assert.That(File.Exists(Backup), Is.False);
            Assert.That(Directory.GetFiles(_directory, "*.tmp*"), Is.Empty);
        }

        [Test]
        public async Task ConcurrentRequestsProduceValidGenerations()
        {
            var manager = Manager();
            await manager.LoadAsync();
            var tasks = new Task[12];
            for (int i = 0; i < tasks.Length; i++) tasks[i] = manager.SaveAsync(new Progress { Coins = i });
            await Task.WhenAll(tasks);
            Assert.That((await manager.LoadAsync()).Status, Is.EqualTo(SaveLoadStatus.Loaded));
            File.WriteAllText(Primary, "{bad");
            Assert.That((await manager.LoadAsync()).Status, Is.EqualTo(SaveLoadStatus.RecoveredBackup));
        }

        [TestCase("../escape")]
        [TestCase("CON")]
        [TestCase("LPT1")]
        [TestCase("bad/name")]
        [TestCase("")]
        public void UnsafeSlotNamesAreRejected(string slot)
        {
            Assert.Throws<ArgumentException>(() => new SaveManager<Progress>(_directory, slot, Definition()));
        }

        [Test]
        public async Task TrailingContentIsRejected()
        {
            var manager = await Seed();
            File.AppendAllText(Primary, "{}");
            Assert.That((await manager.LoadAsync()).Status, Is.EqualTo(SaveLoadStatus.RecoveredBackup));
        }

        [Test]
        public async Task DuplicatePropertiesAreRejected()
        {
            var manager = await Seed();
            File.WriteAllText(Primary, "{\"schemaVersion\":2,\"schemaVersion\":2}");
            Assert.That((await manager.LoadAsync()).Status, Is.EqualTo(SaveLoadStatus.RecoveredBackup));
        }

        [Test]
        public async Task TypeMetadataDoesNotSelectClrTypes()
        {
            var manager = await Seed();
            Edit(root => root["data"]["$type"] = "Not.A.Real.Type, Unknown.Assembly");
            Assert.That((await manager.LoadAsync()).Data.Coins, Is.EqualTo(20));
        }

        [Test]
        public async Task ChangedOnDiskFutureSaveBlocksCommitEvenAfterSuccessfulLoad()
        {
            var manager = await Seed();
            Edit(root => root["schemaVersion"] = 999);
            string before = File.ReadAllText(Primary);
            Assert.ThrowsAsync<SaveCompatibilityException>(async () => await manager.SaveAsync(new Progress()));
            Assert.That(File.ReadAllText(Primary), Is.EqualTo(before));
        }

        [Test]
        public async Task AccidentalObjectReferenceLoopFailsBeforeWriting()
        {
            var definition = new SaveDefinition<RecursiveData>("test.recursive", 1,
                () => new RecursiveData(), data => { });
            var manager = new SaveManager<RecursiveData>(_directory, "slot-1", definition);
            await manager.LoadAsync();
            var snapshot = new RecursiveData();
            snapshot.Child = snapshot;
            Assert.ThrowsAsync<JsonSerializationException>(async () => await manager.SaveAsync(snapshot));
            Assert.That(File.Exists(Primary), Is.False);
        }

        [Test]
        public async Task SequentialMigrationsExecuteInVersionOrder()
        {
            await Seed();
            Edit(root =>
            {
                root["schemaVersion"] = 1;
                ((JObject)root["data"])["Currency"] = 5;
                ((JObject)root["data"]).Remove("Coins");
            });
            var order = new List<int>();
            var definition = new SaveDefinition<Progress>("test.progress", 3, () => new Progress(),
                data => { if (data.Coins < 0) throw new InvalidSaveException("Negative coins."); },
                new Dictionary<int, Action<JObject>>
                {
                    [1] = data =>
                    {
                        order.Add(1);
                        data["Coins"] = data["Currency"].DeepClone();
                        data.Remove("Currency");
                    },
                    [2] = data => { order.Add(2); data["Coins"] = data["Coins"].Value<long>() + 10; }
                });
            var result = await Manager(definition: definition).LoadAsync();
            Assert.That(result.Data.Coins, Is.EqualTo(15));
            Assert.That(order, Is.EqualTo(new[] { 1, 2 }));
        }
    }
}
