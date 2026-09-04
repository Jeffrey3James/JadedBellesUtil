using UnityEngine;

namespace JadedBelles.Util.CharacterCustomization
{
    /// <summary>
    /// Drives a character rig: on <see cref="Start"/>, syncs the visible body parts to the outfit
    /// stored in <see cref="CharacterOutfitManager"/>. Exposes <see cref="GetNextBodyPart"/> and
    /// <see cref="GetLastBodyPart"/> for the UI controller to cycle options per slot.
    /// </summary>
    /// <remarks>
    /// Place this component on the character root; wire the <see cref="bodyParts"/> array in the
    /// inspector with one entry per slot (Hat, Hair, ...) and the swappable GameObjects for each
    /// slot. The rig is fully data-driven: no code changes are needed to add or remove slots.
    /// </remarks>
    public class CharacterCustomization : MonoBehaviour
    {
        [Tooltip("One entry per outfit slot on this character. Slot ids must be unique.")]
        public BodyPart[] bodyParts;

        private async void Start()
        {
            var manager = CharacterOutfitManager.Instance;
            if (manager != null)
            {
                manager.characterCreationEvents.InitializeCharacterOutfits += InitializeBodyParts;
                manager.characterCreationEvents.onChangeBodyPartForward += GetNextBodyPart;

                if (manager.SaveProvider != null)
                {
                    await manager.LoadAsync();
                    InitializeBodyParts();
                    return;
                }
            }

            // No save provider — start with the first option in each slot.
            foreach (var part in bodyParts)
            {
                foreach (var bodyPart in part.bodyParts)
                {
                    bodyPart.SetActive(false);
                }
                if (part.bodyParts.Length > 0)
                {
                    part.bodyParts[0].SetActive(true);
                    part.currentBodyPartIndex = 0;
                }
            }
        }

        private void OnDestroy()
        {
            var manager = CharacterOutfitManager.Instance;
            if (manager?.characterCreationEvents == null) return;
            manager.characterCreationEvents.InitializeCharacterOutfits -= InitializeBodyParts;
            manager.characterCreationEvents.onChangeBodyPartForward -= GetNextBodyPart;
        }

        /// <summary>Refresh the rig from the manager's saved outfit. Called after LoadAsync completes.</summary>
        private void InitializeBodyParts()
        {
            var manager = CharacterOutfitManager.Instance;

            foreach (var part in bodyParts)
            {
                foreach (var bodyPart in part.bodyParts)
                {
                    bodyPart.SetActive(false);
                }
            }

            foreach (var part in bodyParts)
            {
                int savedIndex = manager != null ? manager.GetSavedIndex(part.slotId) : 0;

                if (savedIndex < 0 || savedIndex >= part.bodyParts.Length)
                {
                    Debug.LogWarning($"[CharacterCustomization] Invalid saved index {savedIndex} for slot '{part.slotId}' (only {part.bodyParts.Length} options). Falling back to 0.");
                    savedIndex = 0;
                }

                if (part.bodyParts.Length == 0) continue;
                part.bodyParts[savedIndex].SetActive(true);
                part.currentBodyPartIndex = savedIndex;
            }
        }

        /// <summary>Advance one option forward for a slot, wrapping around at the end.</summary>
        public void GetNextBodyPart(BodyPart part, BodyPart[] bodyParts)
        {
            if (part.bodyParts.Length == 0) return;
            part.bodyParts[part.currentBodyPartIndex].SetActive(false);
            part.currentBodyPartIndex = (part.currentBodyPartIndex + 1) % part.bodyParts.Length;
            part.bodyParts[part.currentBodyPartIndex].SetActive(true);
        }

        /// <summary>Advance one option backward for a slot, wrapping around at the start.</summary>
        public void GetLastBodyPart(BodyPart part)
        {
            if (part.bodyParts.Length == 0) return;
            part.bodyParts[part.currentBodyPartIndex].SetActive(false);
            part.currentBodyPartIndex = (part.currentBodyPartIndex - 1 + part.bodyParts.Length) % part.bodyParts.Length;
            part.bodyParts[part.currentBodyPartIndex].SetActive(true);
        }

        /// <summary>
        /// Copy the current in-scene selection into the manager and persist via the save provider.
        /// Fire-and-forget; the returned task lets callers await the save if needed.
        /// </summary>
        public async System.Threading.Tasks.Task PersistCurrentSelectionAsync()
        {
            var manager = CharacterOutfitManager.Instance;
            if (manager == null) return;
            foreach (var part in bodyParts)
            {
                manager.SetSavedIndex(part.slotId, part.currentBodyPartIndex);
            }
            await manager.SaveAsync();
        }
    }
}
