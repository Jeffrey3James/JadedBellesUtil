using System;

namespace JadedBelles.Util.CharacterCustomization
{
    /// <summary>
    /// Event bus for the character customization flow. Owned by <see cref="CharacterOutfitManager"/>
    /// so a single UI controller can drive a character rig regardless of which scene either lives in.
    /// </summary>
    public class CharacterCreationEvents
    {
        /// <summary>Raised once the outfit save has loaded and the character rig should sync its visuals.</summary>
        public event Action InitializeCharacterOutfits;

        /// <summary>
        /// Trigger the <see cref="InitializeCharacterOutfits"/> event. Typically called by the save
        /// provider once <see cref="ICharacterSaveProvider.LoadOutfitAsync"/> resolves.
        /// </summary>
        public void InitializeOutfits() => InitializeCharacterOutfits?.Invoke();

        /// <summary>Raised when the UI wants the rig to advance one option forward for a given slot.</summary>
        public event Action<BodyPart, BodyPart[]> onChangeBodyPartForward;

        /// <summary>
        /// Trigger the <see cref="onChangeBodyPartForward"/> event.
        /// </summary>
        public void ChangeBodyPart(BodyPart part, BodyPart[] parts) => onChangeBodyPartForward?.Invoke(part, parts);
    }
}
