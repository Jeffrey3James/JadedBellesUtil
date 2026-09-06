using UnityEngine;

namespace JadedBelles.Util.AISenses
{
    /// <summary>
    /// A sound broadcast into the world. Sensors hear it if they're within <see cref="Radius"/> and
    /// the perceived loudness (falloff-scaled) exceeds their threshold.
    /// </summary>
    public readonly struct SoundEvent
    {
        public readonly Vector3 Position;
        public readonly float Radius;
        public readonly float Loudness;
        /// <summary>Optional source transform \u2014 lets sensors filter by faction, e.g. ignore own footsteps.</summary>
        public readonly Transform Source;

        public SoundEvent(Vector3 position, float radius, float loudness, Transform source = null)
        {
            Position = position;
            Radius = radius;
            Loudness = loudness;
            Source = source;
        }
    }

    /// <summary>
    /// A scent puff. Unlike sound, scents linger: the world simulates them via a timed emitter that
    /// re-fires with an intensity that decays over its lifetime. Sensors receive whatever intensity
    /// arrives, apply distance falloff, and optionally bias by a wind direction.
    /// </summary>
    public readonly struct ScentEvent
    {
        public readonly Vector3 Position;
        public readonly float Radius;
        /// <summary>Current strength at the source, 0..1 (already decayed by the emitter).</summary>
        public readonly float Strength;
        /// <summary>Free-form scent tag ("blood", "cookedMeat", "skunk"). Sensors filter on this.</summary>
        public readonly string ScentTag;
        public readonly Transform Source;

        public ScentEvent(Vector3 position, float radius, float strength, string scentTag, Transform source = null)
        {
            Position = position;
            Radius = radius;
            Strength = strength;
            ScentTag = scentTag ?? string.Empty;
            Source = source;
        }
    }

    /// <summary>
    /// A physical contact: something bumped into (or was bumped by) the sensor's collider.
    /// Fired from the sensor's own <c>OnCollisionEnter</c> / <c>OnTriggerEnter</c>; the intensity is
    /// derived from relative velocity for collisions, or a fixed 1 for triggers.
    /// </summary>
    public readonly struct TouchEvent
    {
        public readonly Vector3 Position;
        /// <summary>Perceived force, 0..1 (already scaled by the emitting sensor).</summary>
        public readonly float Intensity;
        /// <summary>The other collider's transform.</summary>
        public readonly Transform Other;
        /// <summary>True if this came from a trigger overlap; false for a physics collision.</summary>
        public readonly bool WasTrigger;

        public TouchEvent(Vector3 position, float intensity, Transform other, bool wasTrigger)
        {
            Position = position;
            Intensity = intensity;
            Other = other;
            WasTrigger = wasTrigger;
        }
    }

    /// <summary>
    /// A tasting event: an ingestion action delivered directly to a sensor (bait, food, poison).
    /// Emitters typically publish via <see cref="TasteEmitter.Feed"/> on a specific sensor rather
    /// than broadcasting globally, but the bus is available for area-of-effect flavors (e.g. water
    /// contaminated across a lake).
    /// </summary>
    public readonly struct TasteEvent
    {
        public readonly Vector3 Position;
        public readonly float Potency;
        /// <summary>Free-form flavor tag ("bait", "poison", "sweet"). Sensors filter on this.</summary>
        public readonly string FlavorTag;
        public readonly Transform Source;

        public TasteEvent(Vector3 position, float potency, string flavorTag, Transform source = null)
        {
            Position = position;
            Potency = potency;
            FlavorTag = flavorTag ?? string.Empty;
            Source = source;
        }
    }
}
