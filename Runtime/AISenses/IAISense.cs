using UnityEngine.Events;

namespace JadedBelles.Util.AISenses
{
    /// <summary>
    /// Marker interface implemented by every JadedBelles AI sensor (sight, hearing, touch, smell,
    /// taste). Kept intentionally minimal \u2014 senses vary too much in signature to share more than
    /// this. Use it to gather every sensor on a character in one <c>GetComponents&lt;IAISense&gt;()</c>
    /// call, e.g. to disable perception while stunned.
    /// </summary>
    public interface IAISense
    {
        /// <summary>True when the sensor is currently listening for stimuli. Disable to stun the sense.</summary>
        bool SenseEnabled { get; set; }
    }

    /// <summary>
    /// A single stimulus reaching a sensor after any per-sense processing (distance falloff, view
    /// cone, decay, etc.). <typeparamref name="TEvent"/> is the raw stimulus type produced by the
    /// emitter; <see cref="Intensity"/> is the sensor's perceived intensity after processing
    /// (typically 0..1).
    /// </summary>
    public readonly struct SensePerception<TEvent>
    {
        public readonly TEvent Event;
        public readonly float Intensity;

        public SensePerception(TEvent evt, float intensity)
        {
            Event = evt;
            Intensity = intensity;
        }
    }

    /// <summary>Signature for a sensor's public "I noticed something" event.</summary>
    /// <typeparam name="TEvent">The per-sense stimulus payload type.</typeparam>
    [System.Serializable]
    public class SensePerceptionEvent<TEvent> : UnityEvent<SensePerception<TEvent>> { }
}
