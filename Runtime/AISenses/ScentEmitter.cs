using UnityEngine;

namespace JadedBelles.Util.AISenses
{
    /// <summary>
    /// Continuously emits <see cref="ScentEvent"/> pulses on a tick interval. Strength decays
    /// linearly from <see cref="initialStrength"/> to zero over <see cref="lifetime"/>; once
    /// exhausted the emitter destroys or disables itself (see <see cref="destroyOnExpire"/>).
    /// </summary>
    /// <remarks>
    /// Spawn one when the blood spills, when the meat starts cooking, when the skunk sprays.
    /// Sensors within radius pick up the pulses via <see cref="StimulusSystem{ScentEvent}"/>.
    /// </remarks>
    public class ScentEmitter : MonoBehaviour
    {
        [Tooltip("Free-form tag sensors filter on: 'blood', 'cookedMeat', 'skunk'.")]
        public string scentTag = "generic";

        [Tooltip("Max distance the scent can reach.")]
        [Min(0f)] public float radius = 8f;

        [Tooltip("Strength at t=0 (0..1). Sensors do further distance falloff on top.")]
        [Range(0f, 1f)] public float initialStrength = 1f;

        [Tooltip("Seconds until strength decays to 0.")]
        [Min(0f)] public float lifetime = 30f;

        [Tooltip("Seconds between pulses. Cheaper than every-frame emission; sensors need only occasional refresh.")]
        [Min(0.05f)] public float pulseInterval = 0.5f;

        [Tooltip("Destroy the emitter GameObject when lifetime hits 0. Otherwise the component just disables itself.")]
        public bool destroyOnExpire = true;

        private float elapsed;
        private float nextPulseAt;

        private void OnEnable()
        {
            elapsed = 0f;
            nextPulseAt = 0f;
        }

        private void Update()
        {
            elapsed += UnityEngine.Time.deltaTime;
            if (elapsed >= lifetime)
            {
                if (destroyOnExpire) Destroy(gameObject);
                else enabled = false;
                return;
            }

            if (UnityEngine.Time.time < nextPulseAt) return;
            nextPulseAt = UnityEngine.Time.time + pulseInterval;

            float t = elapsed / lifetime;
            float currentStrength = initialStrength * (1f - t);
            StimulusSystem<ScentEvent>.Emit(new ScentEvent(
                transform.position, radius, currentStrength, scentTag, transform));
        }

        private void OnDrawGizmosSelected()
        {
            float t = Application.isPlaying && lifetime > 0f ? Mathf.Clamp01(elapsed / lifetime) : 0f;
            float alpha = 0.5f * (1f - t);
            Gizmos.color = new UnityEngine.Color(0.7f, 0.3f, 0.9f, alpha);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
