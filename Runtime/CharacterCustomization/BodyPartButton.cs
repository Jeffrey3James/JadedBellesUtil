using System;
using UnityEngine.UI;

namespace JadedBelles.Util.CharacterCustomization
{
    /// <summary>
    /// Serializable descriptor wiring a UGUI <see cref="Button"/> to a specific outfit slot and a
    /// direction (next or previous). Populated in the inspector; consumed by
    /// <see cref="CharacterUIController"/>.
    /// </summary>
    [Serializable]
    public class BodyPartButton
    {
        public Button button;

        /// <summary>Free-form id matching <see cref="BodyPart.slotId"/> on the character rig.</summary>
        public string slotId;

        /// <summary>True = advance forward on click, false = advance backward.</summary>
        public bool next;
    }
}
