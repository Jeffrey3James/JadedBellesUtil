using System.Collections.Generic;
using System.Threading.Tasks;

namespace JadedBelles.Util.CharacterCustomization
{
    /// <summary>
    /// Bridge between <see cref="CharacterOutfitManager"/> and whatever save system the game uses
    /// (cloud save, PlayerPrefs, backend API, local JSON, etc.). Implement this once per game and
    /// assign an instance on the manager at boot.
    /// </summary>
    /// <remarks>
    /// The dictionary is keyed by <see cref="BodyPart.slotId"/> and holds the currently selected
    /// index for each slot. Implementations should treat a missing key as "use the default" (0).
    /// </remarks>
    public interface ICharacterSaveProvider
    {
        /// <summary>
        /// Pull the player's saved outfit from storage. Returns an empty dictionary if the player
        /// has never customized before.
        /// </summary>
        Task<Dictionary<string, int>> LoadOutfitAsync();

        /// <summary>
        /// Persist the player's current outfit. Callers await this so a scene transition can
        /// happen only after the save completes; fire-and-forget implementations may return
        /// <see cref="Task.CompletedTask"/> immediately.
        /// </summary>
        Task SaveOutfitAsync(Dictionary<string, int> outfitBySlotId);
    }
}
