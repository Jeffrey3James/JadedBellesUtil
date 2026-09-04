using System.Collections.Generic;
using JadedBelles.Util.Singletons;
using UnityEngine;

namespace JadedBelles.Util.CharacterCustomization
{
    /// <summary>
    /// Persistent singleton that owns the player's outfit selection across scenes and hands out
    /// the shared <see cref="CharacterCreationEvents"/> bus. Assign an <see cref="ICharacterSaveProvider"/>
    /// implementation at boot (or leave null to run without persistence).
    /// </summary>
    /// <remarks>
    /// Outfit state is keyed by <see cref="BodyPart.slotId"/> so scenes can add or remove slots
    /// without invalidating saves for unrelated slots.
    /// </remarks>
    public class CharacterOutfitManager : SingletonBehaviour<CharacterOutfitManager>
    {
        /// <summary>Save provider used by <see cref="LoadAsync"/> and <see cref="SaveAsync"/>. Optional.</summary>
        public ICharacterSaveProvider SaveProvider { get; set; }

        /// <summary>Currently selected index per slot. Cleared on scene reload only if this instance is destroyed.</summary>
        public Dictionary<string, int> OutfitBySlotId { get; private set; } = new Dictionary<string, int>();

        /// <summary>Event bus consumed by <see cref="CharacterCustomization"/> rigs and UI controllers.</summary>
        public CharacterCreationEvents characterCreationEvents;

        protected override void OnSingletonAwake()
        {
            if (characterCreationEvents == null)
            {
                characterCreationEvents = new CharacterCreationEvents();
            }
        }

        /// <summary>Pull the outfit from <see cref="SaveProvider"/> (no-op if the provider is null).</summary>
        public async System.Threading.Tasks.Task LoadAsync()
        {
            if (SaveProvider == null) return;
            OutfitBySlotId = await SaveProvider.LoadOutfitAsync() ?? new Dictionary<string, int>();
        }

        /// <summary>Push the outfit to <see cref="SaveProvider"/> (no-op if the provider is null).</summary>
        public async System.Threading.Tasks.Task SaveAsync()
        {
            if (SaveProvider == null) return;
            await SaveProvider.SaveOutfitAsync(OutfitBySlotId);
        }

        /// <summary>Look up the saved index for a slot, defaulting to 0 if the slot was never saved.</summary>
        public int GetSavedIndex(string slotId)
        {
            return OutfitBySlotId.TryGetValue(slotId, out var idx) ? idx : 0;
        }

        /// <summary>Set the saved index for a slot in-memory. Call <see cref="SaveAsync"/> to persist.</summary>
        public void SetSavedIndex(string slotId, int index)
        {
            OutfitBySlotId[slotId] = index;
        }
    }
}
