using System;
using System.IO;
using UnityEngine;

namespace JadedBelles.Util.SaveSystem
{
    /// <summary>Main-thread factory. Does not create a scene singleton or cloud connection.</summary>
    public static class UnitySaveSystem
    {
        /// <summary>
        /// Creates a manager under persistentDataPath/Saves/product/profile/slot.json.
        /// Use a stable, non-secret internal profile ID, or "guest". All segments are validated.
        /// </summary>
        public static SaveManager<T> Create<T>(string product, string profile, string slot,
            SaveDefinition<T> definition) where T : class
        {
            SaveManager<T>.ValidatePathSegment(product, nameof(product));
            SaveManager<T>.ValidatePathSegment(profile, nameof(profile));
            SaveManager<T>.ValidatePathSegment(slot, nameof(slot));
            string root = Application.persistentDataPath;
            if (string.IsNullOrWhiteSpace(root))
                throw new PlatformNotSupportedException("This platform has no persistent data path.");
            return new SaveManager<T>(Path.Combine(root, "Saves", product, profile), slot, definition);
        }
    }
}
