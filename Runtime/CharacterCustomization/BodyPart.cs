using System;
using UnityEngine;

namespace JadedBelles.Util.CharacterCustomization
{
    /// <summary>
    /// A single slot in a character's outfit (hat, hair, shoes, wings, etc.) plus the list of
    /// swappable GameObjects for that slot. Exactly one entry in <see cref="bodyParts"/> is
    /// active at a time; <see cref="currentBodyPartIndex"/> is the index of the active one.
    /// </summary>
    /// <remarks>
    /// <see cref="slotId"/> is a free-form string chosen by the consuming game. Common ids are
    /// "Hat", "Hair", "Torso", "Pants", "Shoes" — but a fashion game might use "Necklace",
    /// "Earrings", "Purse". The util does not assume a fixed set of slots.
    /// </remarks>
    [Serializable]
    public class BodyPart
    {
        [Tooltip("Free-form slot identifier (e.g. \"Hat\", \"Hair\"). Must be unique within a character.")]
        public string slotId;

        [Tooltip("All swappable GameObjects for this slot. Only one is active at a time.")]
        public GameObject[] bodyParts;

        [HideInInspector]
        public int currentBodyPartIndex;
    }
}
