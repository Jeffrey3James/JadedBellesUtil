using System.Collections.Generic;
using UnityEngine;

namespace JadedBelles.Util.RoomTemplates
{
    /// <summary>
    /// Author-time helper for building a <see cref="RoomTemplate"/> visually in a scene. Drop this
    /// on a child GameObject of the room's origin transform, set <see cref="slotName"/> and
    /// candidate prefabs, and size the attached <see cref="BoxCollider"/> to define the zone.
    /// The Room Randomizer editor window can snapshot every SlotZoneAuthoring under an origin into
    /// a <see cref="RoomTemplate"/> asset.
    /// </summary>
    /// <remarks>
    /// The collider is used only for its size; the SlotZoneAuthoring GameObject is not part of the
    /// randomizer's runtime overlap checks. Marking the collider as trigger avoids polluting scene
    /// physics; the snapshot reads its bounds regardless.
    /// </remarks>
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public class SlotZoneAuthoring : MonoBehaviour
    {
        [Tooltip("Human-readable slot name, e.g. 'Bed', 'Nightstand'.")]
        public string slotName = "Slot";

        [Tooltip("Prefabs that can fill this slot, with per-prefab weights.")]
        public List<SlotPrefabCandidate> candidatePrefabs = new();

        [Tooltip("Inclusive range for how many prefabs to place in this slot. x=min, y=max.")]
        public Vector2Int countRange = new Vector2Int(1, 1);

        [Tooltip("How each placed piece is rotated.")]
        public SlotRotationMode rotationMode = SlotRotationMode.RandomYaw;

        [Min(1)]
        [Tooltip("Maximum overlap-check retries per piece before giving up on this piece.")]
        public int maxPlacementAttempts = 12;

        [Tooltip("Optional extra buffer added to each placed prefab's collider bounds during overlap tests.")]
        public float overlapPadding = 0f;

        [Tooltip("Gizmo color for the zone box in the Scene View.")]
        public Color gizmoColor = new Color(0f, 1f, 0.5f, 0.35f);

        private void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider>();
            if (col == null) return;

            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);

            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(col.center, col.size);
        }
    }
}
