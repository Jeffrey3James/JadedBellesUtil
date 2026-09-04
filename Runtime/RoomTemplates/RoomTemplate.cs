using System;
using System.Collections.Generic;
using UnityEngine;

namespace JadedBelles.Util.RoomTemplates
{
    /// <summary>
    /// Rotation options for a randomized room slot placement.
    /// </summary>
    public enum SlotRotationMode
    {
        /// <summary>Keep the prefab's authored rotation exactly.</summary>
        None = 0,
        /// <summary>Rotate randomly around Y (standard for furniture on a floor).</summary>
        RandomYaw = 1,
        /// <summary>Rotate randomly on all three axes.</summary>
        RandomAll = 2,
        /// <summary>Pick one of four yaw angles: 0, 90, 180, 270 degrees.</summary>
        RandomYaw90 = 3
    }

    /// <summary>
    /// One weighted prefab candidate inside a <see cref="RoomSlot"/>. Higher weights are picked more often.
    /// </summary>
    [Serializable]
    public class SlotPrefabCandidate
    {
        public GameObject prefab;

        [Min(0f)]
        [Tooltip("Relative weight when picking one of the slot's candidates. 0 = never; leave at 1 for uniform.")]
        public float weight = 1f;
    }

    /// <summary>
    /// One authored slot in a <see cref="RoomTemplate"/>. Defines a local-space box that valid placements
    /// must sit inside, the candidate prefabs that can fill it, how many to spawn, and how they may rotate.
    /// </summary>
    [Serializable]
    public class RoomSlot
    {
        [Tooltip("Human-readable slot name, e.g. 'Bed', 'Nightstand', 'RugCenter'. Used in logs and gizmos only.")]
        public string slotName = "Slot";

        [Tooltip("Zone the piece can spawn inside, expressed in the ROOM ORIGIN's local space. " +
                 "The randomizer samples a random point inside these bounds and places the prefab there.")]
        public Bounds localZone = new Bounds(Vector3.zero, Vector3.one);

        [Tooltip("Prefabs that can fill this slot, with per-prefab weights. Leave empty to skip the slot.")]
        public List<SlotPrefabCandidate> candidatePrefabs = new();

        [Tooltip("Inclusive range for how many prefabs to place in this slot. x=min, y=max.")]
        public Vector2Int countRange = new Vector2Int(1, 1);

        [Tooltip("How each placed piece is rotated.")]
        public SlotRotationMode rotationMode = SlotRotationMode.RandomYaw;

        [Min(1)]
        [Tooltip("Maximum overlap-check retries per piece before giving up on this piece.")]
        public int maxPlacementAttempts = 12;

        [Tooltip("Optional extra buffer added to each placed prefab's collider bounds during overlap tests. " +
                 "Positive values push placements further apart; keep at 0 for tight packing.")]
        public float overlapPadding = 0f;
    }

    /// <summary>
    /// A named room layout template (e.g. "Bedroom"): a list of <see cref="RoomSlot"/> entries that a
    /// randomizer fills with prefabs while respecting collider overlaps. Author with the
    /// <c>SlotZoneAuthoring</c> component in a scene, then snapshot into an asset from the
    /// <c>Tools &gt; JadedBelles &gt; Room Randomizer</c> window; or edit slot bounds directly here.
    /// </summary>
    [CreateAssetMenu(menuName = "JadedBelles/Room Template", fileName = "RoomTemplate")]
    public class RoomTemplate : ScriptableObject
    {
        [Tooltip("Display name, e.g. 'Bedroom', 'Kitchen', 'Boss Arena'.")]
        public string templateName = "Bedroom";

        [TextArea(2, 5)]
        public string notes = "";

        public List<RoomSlot> slots = new();
    }
}
