using UnityEngine;

namespace JadedBelles.Util.AISenses
{
    /// <summary>
    /// Which sense a distraction pings when triggered. A single distraction can ping multiple senses
    /// (a thrown flare emits light, sound, and heat), so this is a [Flags] enum.
    /// </summary>
    [System.Flags]
    public enum DistractionSense
    {
        None = 0,
        Sound = 1 << 0,
        Scent = 1 << 1,
        /// <summary>Reserved for future light-based sensor \u2014 currently ignored by the built-in senses.</summary>
        Light = 1 << 2,
    }

    /// <summary>
    /// Authoring data for a distraction: everything a <see cref="HoldableDistraction"/> or
    /// <see cref="ThrowableDistraction"/> needs to know when it fires. Reusable across items
    /// (all wine bottles broadcast the same distraction).
    /// </summary>
    /// <remarks>
    /// Extracted and generalized from SyntyGameJam's per-item <c>DistractionData</c>. Two changes:
    /// <list type="bullet">
    ///   <item>The <c>AudioClip</c> field is gone \u2014 audio playback belongs in a separate
    ///     component. This class is data for the AI senses only.</item>
    ///   <item>New <see cref="senses"/> mask lets one distraction ping multiple senses at once.</item>
    /// </list>
    /// </remarks>
    [System.Serializable]
    public class DistractionData
    {
        public string label = "Distraction";

        [Tooltip("Which senses this distraction pings.")]
        public DistractionSense senses = DistractionSense.Sound;

        [Header("Sound (used when senses includes Sound)")]
        [Range(0f, 1f)] public float loudness = 0.8f;
        [Min(0f)] public float soundRadius = 8f;

        [Header("Scent (used when senses includes Scent)")]
        public string scentTag = "distraction";
        [Range(0f, 1f)] public float scentStrength = 0.6f;
        [Min(0f)] public float scentRadius = 6f;
        [Min(0f)] public float scentLifetime = 8f;

        /// <summary>
        /// Convenience: broadcast this distraction from a world position using the configured
        /// sense mask. Scent pulses are emitted once directly (call <see cref="SpawnScentEmitter"/>
        /// instead if you want the scent to linger and decay).
        /// </summary>
        public void EmitFrom(Vector3 position, Transform source = null)
        {
            if ((senses & DistractionSense.Sound) != 0)
            {
                Stimuli.EmitSound(new SoundEvent(position, soundRadius, loudness, source));
            }
            if ((senses & DistractionSense.Scent) != 0)
            {
                Stimuli.EmitScent(new ScentEvent(position, scentRadius, scentStrength, scentTag, source));
            }
        }

        /// <summary>
        /// Spawn a <see cref="ScentEmitter"/> at the given world position so the smell lingers and
        /// decays. Returns null when the distraction doesn't include the Scent sense.
        /// </summary>
        public ScentEmitter SpawnScentEmitter(Vector3 position, Transform parent = null)
        {
            if ((senses & DistractionSense.Scent) == 0) return null;
            var go = new GameObject($"Scent[{scentTag}]");
            go.transform.SetParent(parent, worldPositionStays: true);
            go.transform.position = position;
            var emitter = go.AddComponent<ScentEmitter>();
            emitter.scentTag = scentTag;
            emitter.radius = scentRadius;
            emitter.initialStrength = scentStrength;
            emitter.lifetime = scentLifetime;
            return emitter;
        }
    }
}
