using System.Collections.Generic;
using UnityEngine;

namespace JadedBelles.Util.RoomTemplates
{
    /// <summary>
    /// Options passed to <see cref="RoomRandomizer.Randomize"/>. Kept as a mutable config object
    /// (rather than a long parameter list) so callers can preset a config once and reuse it.
    /// </summary>
    public class RoomRandomizerOptions
    {
        /// <summary>Template to fill. Required.</summary>
        public RoomTemplate template;

        /// <summary>Transform whose position + rotation is the room origin. Slot zones are relative to this.</summary>
        public Transform origin;

        /// <summary>Parent transform for spawned prefabs. If null, spawns at scene root.</summary>
        public Transform container;

        /// <summary>Physics layers to overlap-check against. Everything by default; set narrower to ignore terrain, etc.</summary>
        public LayerMask overlapLayers = ~0;

        /// <summary>Optional seed for reproducible layouts. When null, uses <c>Random.state</c> as-is.</summary>
        public int? seed;

        /// <summary>Called with each successful placement.</summary>
        public System.Action<GameObject, RoomSlot> onPlaced;

        /// <summary>Called with the slot when the randomizer gives up on placing a piece there.</summary>
        public System.Action<RoomSlot> onPlacementFailed;
    }

    /// <summary>
    /// Fills a <see cref="RoomTemplate"/> with prefabs at runtime or in the editor, using
    /// <c>Physics.OverlapBox</c> to prevent overlaps. Purely a service class \u2014 no scene state.
    /// </summary>
    public static class RoomRandomizer
    {
        /// <summary>
        /// Randomize the given template into the scene. Returns every GameObject that was placed.
        /// Callers own the results and can pool/destroy them however they want.
        /// </summary>
        public static List<GameObject> Randomize(RoomRandomizerOptions options)
        {
            var placed = new List<GameObject>();
            if (options == null || options.template == null || options.origin == null) return placed;

            Random.State? savedState = null;
            if (options.seed.HasValue)
            {
                savedState = Random.state;
                Random.InitState(options.seed.Value);
            }

            foreach (var slot in options.template.slots)
            {
                if (slot == null) continue;
                if (slot.candidatePrefabs == null || slot.candidatePrefabs.Count == 0) continue;

                int min = Mathf.Min(slot.countRange.x, slot.countRange.y);
                int max = Mathf.Max(slot.countRange.x, slot.countRange.y);
                int count = Random.Range(min, max + 1);

                for (int i = 0; i < count; i++)
                {
                    if (!TryPlaceOne(slot, options, out GameObject spawned))
                    {
                        options.onPlacementFailed?.Invoke(slot);
                        continue;
                    }

                    placed.Add(spawned);
                    options.onPlaced?.Invoke(spawned, slot);
                }
            }

            if (savedState.HasValue) Random.state = savedState.Value;
            return placed;
        }

        private static bool TryPlaceOne(RoomSlot slot, RoomRandomizerOptions options, out GameObject spawned)
        {
            spawned = null;

            GameObject prefab = PickWeighted(slot.candidatePrefabs);
            if (prefab == null) return false;

            for (int attempt = 0; attempt < slot.maxPlacementAttempts; attempt++)
            {
                Vector3 localPoint = RandomInsideBounds(slot.localZone);
                Vector3 worldPoint = options.origin.TransformPoint(localPoint);
                Quaternion worldRot = options.origin.rotation * RandomRotationFor(slot.rotationMode);

                if (WouldOverlap(prefab, worldPoint, worldRot, slot.overlapPadding, options.overlapLayers)) continue;

                spawned = Object.Instantiate(prefab, worldPoint, worldRot, options.container);
                return true;
            }

            return false;
        }

        /// <summary>Weighted pick. Skips 0-weight and null-prefab entries.</summary>
        public static GameObject PickWeighted(List<SlotPrefabCandidate> candidates)
        {
            float total = 0f;
            foreach (var c in candidates)
            {
                if (c == null || c.prefab == null || c.weight <= 0f) continue;
                total += c.weight;
            }
            if (total <= 0f) return null;

            float pick = Random.value * total;
            float running = 0f;
            foreach (var c in candidates)
            {
                if (c == null || c.prefab == null || c.weight <= 0f) continue;
                running += c.weight;
                if (pick <= running) return c.prefab;
            }
            return null;
        }

        /// <summary>Sample a uniformly random point inside the given local-space AABB.</summary>
        public static Vector3 RandomInsideBounds(Bounds b)
        {
            return new Vector3(
                Random.Range(b.min.x, b.max.x),
                Random.Range(b.min.y, b.max.y),
                Random.Range(b.min.z, b.max.z)
            );
        }

        /// <summary>Pick a rotation based on the slot's mode. Returns local rotation (composed with origin later).</summary>
        public static Quaternion RandomRotationFor(SlotRotationMode mode)
        {
            switch (mode)
            {
                case SlotRotationMode.None:
                    return Quaternion.identity;
                case SlotRotationMode.RandomYaw:
                    return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                case SlotRotationMode.RandomYaw90:
                    return Quaternion.Euler(0f, 90f * Random.Range(0, 4), 0f);
                case SlotRotationMode.RandomAll:
                    return Random.rotationUniform;
                default:
                    return Quaternion.identity;
            }
        }

        /// <summary>
        /// Compute the world-space bounds a prefab WOULD have if placed at <paramref name="position"/>
        /// with <paramref name="rotation"/>, then run <c>Physics.OverlapBox</c> to check for hits.
        /// Uses the prefab's Renderer bounds when no colliders exist, so decor prefabs without physics
        /// still get spaced out sanely.
        /// </summary>
        public static bool WouldOverlap(GameObject prefab, Vector3 position, Quaternion rotation, float padding, LayerMask layers)
        {
            Bounds local = ComputeLocalBounds(prefab);
            Vector3 halfExtents = local.extents + Vector3.one * padding;
            if (halfExtents.x <= 0f || halfExtents.y <= 0f || halfExtents.z <= 0f) return false;

            Vector3 worldCenter = position + rotation * local.center;
            var hits = Physics.OverlapBox(worldCenter, halfExtents, rotation, layers, QueryTriggerInteraction.Ignore);
            return hits != null && hits.Length > 0;
        }

        /// <summary>
        /// Union of every Collider (falling back to Renderer) bounds on the prefab, expressed in the
        /// prefab's local space. Returns a zero-sized bounds when the prefab has neither.
        /// </summary>
        public static Bounds ComputeLocalBounds(GameObject prefab)
        {
            var colliders = prefab.GetComponentsInChildren<Collider>(includeInactive: false);
            if (colliders != null && colliders.Length > 0)
            {
                return BoundsFromComponents<Collider>(prefab.transform, colliders, c => c.bounds);
            }

            var renderers = prefab.GetComponentsInChildren<Renderer>(includeInactive: false);
            if (renderers != null && renderers.Length > 0)
            {
                return BoundsFromComponents<Renderer>(prefab.transform, renderers, r => r.bounds);
            }

            return new Bounds(Vector3.zero, Vector3.zero);
        }

        private static Bounds BoundsFromComponents<T>(Transform root, T[] components, System.Func<T, Bounds> getWorldBounds) where T : Component
        {
            bool started = false;
            Bounds worldUnion = new Bounds(Vector3.zero, Vector3.zero);

            foreach (var c in components)
            {
                if (c == null) continue;
                var b = getWorldBounds(c);
                if (!started) { worldUnion = b; started = true; }
                else worldUnion.Encapsulate(b);
            }

            if (!started) return new Bounds(Vector3.zero, Vector3.zero);

            Vector3 localCenter = root.InverseTransformPoint(worldUnion.center);
            return new Bounds(localCenter, worldUnion.size);
        }
    }
}
