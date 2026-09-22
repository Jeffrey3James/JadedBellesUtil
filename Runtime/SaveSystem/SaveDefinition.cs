using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace JadedBelles.Util.SaveSystem
{
    /// <summary>Immutable save contract supplied by the consuming game.</summary>
    public sealed class SaveDefinition<T> where T : class
    {
        private readonly Dictionary<int, Action<JObject>> _migrations;

        public string SchemaId { get; }
        public int CurrentVersion { get; }
        public int MaximumBytes { get; }
        public int MaximumDepth { get; }
        internal Func<T> CreateDefault { get; }
        internal Action<T> Validate { get; }

        /// <param name="schemaId">Stable contract ID, such as "my-game.progress". Never a CLR type name.</param>
        /// <param name="createDefault">Called on a worker only when both save generations are absent.</param>
        /// <param name="validate">Worker-safe validator. Throw InvalidSaveException for invalid data.</param>
        /// <param name="migrations">Entry N transforms the data object from version N to N+1 in place.</param>
        public SaveDefinition(string schemaId, int currentVersion, Func<T> createDefault,
            Action<T> validate, IDictionary<int, Action<JObject>> migrations = null,
            int maximumBytes = 16 * 1024 * 1024, int maximumDepth = 48)
        {
            if (string.IsNullOrWhiteSpace(schemaId)) throw new ArgumentException("Schema ID is required.", nameof(schemaId));
            if (currentVersion < 1) throw new ArgumentOutOfRangeException(nameof(currentVersion));
            if (maximumBytes < 1) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
            if (maximumDepth < 4 || maximumDepth > 256)
                throw new ArgumentOutOfRangeException(nameof(maximumDepth), "Use a depth between 4 and 256.");
            SchemaId = schemaId;
            CurrentVersion = currentVersion;
            MaximumBytes = maximumBytes;
            MaximumDepth = maximumDepth;
            CreateDefault = createDefault ?? throw new ArgumentNullException(nameof(createDefault));
            Validate = validate ?? throw new ArgumentNullException(nameof(validate));
            _migrations = migrations == null
                ? new Dictionary<int, Action<JObject>>()
                : new Dictionary<int, Action<JObject>>(migrations);
            foreach (var entry in _migrations)
                if (entry.Key < 1 || entry.Key >= currentVersion || entry.Value == null)
                    throw new ArgumentException("Migration keys must be supported source versions.", nameof(migrations));
        }

        internal void Migrate(int fromVersion, JObject data)
        {
            if (!_migrations.TryGetValue(fromVersion, out var migrate))
                throw new SaveCompatibilityException("No migration from schema version " + fromVersion + ".");
            migrate(data);
        }

        internal void ValidateData(T data)
        {
            if (data == null) throw new InvalidSaveException("The save payload is null.");
            Validate(data);
        }
    }
}
