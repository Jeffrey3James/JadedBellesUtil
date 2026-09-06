using UnityEngine;

namespace JadedBelles.Util.AISenses
{
    /// <summary>
    /// Physical-contact sensor. Fires <see cref="OnTouched"/> when a collider enters this
    /// sensor's collider, whether via <c>OnCollisionEnter</c> (solid contact, intensity scales with
    /// relative velocity) or <c>OnTriggerEnter</c> (overlap, fixed intensity = 1).
    /// </summary>
    /// <remarks>
    /// Use cases: a guard notices being bumped from behind, a pet notices being petted, a landmine
    /// notices being stepped on, a spooky mannequin notices being touched by the player.
    /// <para>
    /// Attach to a GameObject with a Collider (trigger or solid, either works). The
    /// <see cref="ignoreLayers"/> mask filters unwanted contacts (e.g. your own limbs).
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(Collider))]
    public class TouchSensor : MonoBehaviour, IAISense
    {
        [Header("Filtering")]
        [Tooltip("Colliders on any of these layers are ignored entirely.")]
        public LayerMask ignoreLayers = 0;

        [Tooltip("Minimum perceived intensity (0..1) to fire OnTouched. Trigger overlaps always fire (intensity = 1).")]
        [Range(0f, 1f)]
        [SerializeField] private float touchThreshold = 0f;

        [Tooltip("Relative velocity that maps to intensity = 1 for collisions. Higher = more force needed.")]
        [Min(0.01f)]
        [SerializeField] private float saturationVelocity = 8f;

        [Header("Enabled")]
        [SerializeField] private bool senseEnabled = true;

        [Header("Global publish")]
        [Tooltip("If true, ALSO broadcast every touch to StimulusSystem<TouchEvent> for global listeners. Off by default \u2014 touches are usually private.")]
        [SerializeField] private bool alsoBroadcast = false;

        [Header("Events")]
        public SensePerceptionEvent<TouchEvent> OnTouched;

        public bool SenseEnabled { get => senseEnabled; set => senseEnabled = value; }

        private void OnCollisionEnter(Collision collision)
        {
            if (!senseEnabled) return;
            if (IsIgnored(collision.collider)) return;

            float intensity = Mathf.Clamp01(collision.relativeVelocity.magnitude / saturationVelocity);
            if (intensity < touchThreshold) return;

            var contact = collision.GetContact(0).point;
            Fire(new TouchEvent(contact, intensity, collision.transform, wasTrigger: false));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!senseEnabled) return;
            if (IsIgnored(other)) return;

            Fire(new TouchEvent(other.transform.position, 1f, other.transform, wasTrigger: true));
        }

        private bool IsIgnored(Collider col)
        {
            if (col == null) return true;
            return (ignoreLayers.value & (1 << col.gameObject.layer)) != 0;
        }

        private void Fire(TouchEvent evt)
        {
            OnTouched?.Invoke(new SensePerception<TouchEvent>(evt, evt.Intensity));
            if (alsoBroadcast) StimulusSystem<TouchEvent>.Emit(evt);
        }
    }
}
