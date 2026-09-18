using System.Collections.Generic;
using JadedBelles.Util.SaveSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JadedBelles.Util.Samples.SaveSystemBasics
{
    // Game-specific types belong in a game or sample, never in the generic runtime.
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SampleSaveData
    {
        [JsonProperty(Required = Required.Always)] public PlayerState Player = new PlayerState();
        [JsonProperty(Required = Required.Always)] public WorldData World = new WorldData();

        public static SaveDefinition<SampleSaveData> CreateDefinition()
        {
            return new SaveDefinition<SampleSaveData>(
                "save-sample.progress", 2, () => new SampleSaveData(), Validate,
                new Dictionary<int, System.Action<JObject>> { [1] = MigrateVersionOne });
        }

        private static void MigrateVersionOne(JObject data)
        {
            var player = data["Player"] as JObject;
            if (player == null || player["Currency"]?.Type != JTokenType.Integer || player["Coins"] != null)
                throw new InvalidSaveException("Invalid version-one player data.");
            player["Coins"] = player["Currency"].DeepClone();
            player.Remove("Currency");
            if (player["QuestProgress"] == null) player["QuestProgress"] = new JObject();
        }

        private static void Validate(SampleSaveData data)
        {
            if (data.Player == null || data.World == null || data.Player.Level < 1 || data.Player.Coins < 0 ||
                data.Player.Inventory == null || data.Player.QuestProgress == null ||
                data.World.Entities == null || data.World.Flags == null ||
                string.IsNullOrWhiteSpace(data.World.SceneId))
                throw new InvalidSaveException("Missing or invalid player/world data.");
            ValidatePosition(data.Player.Position);
            foreach (var item in data.Player.Inventory)
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId) || item.Quantity <= 0)
                    throw new InvalidSaveException("Invalid inventory item.");
            foreach (var quest in data.Player.QuestProgress)
                if (string.IsNullOrWhiteSpace(quest.Key) || quest.Value < 0)
                    throw new InvalidSaveException("Invalid quest progress.");
            foreach (var pair in data.World.Entities)
            {
                var entity = pair.Value;
                if (entity == null || string.IsNullOrWhiteSpace(pair.Key) ||
                    entity.EntityId != pair.Key || entity.LinkedEntityIds == null)
                    throw new InvalidSaveException("Invalid world entity.");
                ValidatePosition(entity.Position);
                foreach (var id in entity.LinkedEntityIds)
                    if (id == null || !data.World.Entities.ContainsKey(id))
                        throw new InvalidSaveException("Dangling entity reference.");
            }
        }

        private static void ValidatePosition(PositionData value)
        {
            if (value == null || float.IsNaN(value.X) || float.IsInfinity(value.X) ||
                float.IsNaN(value.Y) || float.IsInfinity(value.Y) ||
                float.IsNaN(value.Z) || float.IsInfinity(value.Z))
                throw new InvalidSaveException("Invalid position.");
        }

        // Call on the main thread; update DTO state from scene objects BEFORE copying.
        public SampleSaveData CaptureSnapshot()
        {
            var snapshot = new SampleSaveData();
            snapshot.Player.Level = Player.Level;
            snapshot.Player.Coins = Player.Coins;
            snapshot.Player.Position = Player.Position.Copy();
            foreach (var item in Player.Inventory)
                snapshot.Player.Inventory.Add(new ItemStack { ItemId = item.ItemId, Quantity = item.Quantity });
            foreach (var quest in Player.QuestProgress) snapshot.Player.QuestProgress.Add(quest.Key, quest.Value);
            snapshot.World.SceneId = World.SceneId;
            foreach (var flag in World.Flags) snapshot.World.Flags.Add(flag.Key, flag.Value);
            foreach (var pair in World.Entities)
                snapshot.World.Entities.Add(pair.Key, new EntityState
                {
                    EntityId = pair.Value.EntityId,
                    Position = pair.Value.Position.Copy(),
                    LinkedEntityIds = new List<string>(pair.Value.LinkedEntityIds)
                });
            return snapshot;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class PlayerState
    {
        [JsonProperty(Required = Required.Always)] public int Level = 1;
        [JsonProperty(Required = Required.Always)] public long Coins;
        [JsonProperty(Required = Required.Always)] public PositionData Position = new PositionData();
        [JsonProperty(Required = Required.Always)] public List<ItemStack> Inventory = new List<ItemStack>();
        [JsonProperty(Required = Required.Always)] public Dictionary<string, int> QuestProgress =
            new Dictionary<string, int>();
        [JsonIgnore] public WorldData RuntimeWorld;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class WorldData
    {
        [JsonProperty(Required = Required.Always)] public string SceneId = "start";
        [JsonProperty(Required = Required.Always)] public Dictionary<string, EntityState> Entities =
            new Dictionary<string, EntityState>();
        [JsonProperty(Required = Required.Always)] public Dictionary<string, bool> Flags =
            new Dictionary<string, bool>();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EntityState
    {
        [JsonProperty(Required = Required.Always)] public string EntityId;
        [JsonProperty(Required = Required.Always)] public PositionData Position = new PositionData();
        [JsonProperty(Required = Required.Always)] public List<string> LinkedEntityIds = new List<string>();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class PositionData
    {
        [JsonProperty(Required = Required.Always)] public float X;
        [JsonProperty(Required = Required.Always)] public float Y;
        [JsonProperty(Required = Required.Always)] public float Z;
        public PositionData Copy() => new PositionData { X = X, Y = Y, Z = Z };
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class ItemStack
    {
        [JsonProperty(Required = Required.Always)] public string ItemId;
        [JsonProperty(Required = Required.Always)] public int Quantity;
    }
}
