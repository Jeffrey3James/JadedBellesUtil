using UnityEngine;

namespace JadedBelles.Util.AISenses
{
    /// <summary>
    /// Event-driven hearing sensor. Subscribes to <see cref="StimulusSystem{SoundEvent}"/>; on each
    /// broadcast, computes the perceived loudness as
    /// <c>event.Loudness * (1 - distance/event.Radius)</c>, discards it when below
    /// <see cref="hearingThreshold"/> or the source is the sensor's own ignored transform, and
    /// otherwise invokes <see cref="OnHeard"/>.
    /// </summary>
    /// <remarks>
    /// Extracted from SyntyGameJam's <c>HearingSensor</c>. Cleaned up:
    /// <list type="bullet">
    ///   <item>The always-on <c>Debug.Log</c> pair is gone.</item>
    ///   <item>New <see cref="ignoreSource"/> field: drag in the character's own transform so it
    ///     doesn't hear its own footsteps.</item>
    ///   <item>Now implements <see cref="IAISense"/> and honors <see cref="SenseEnabled"/>.</item>
    ///   <item>Public event exposed as <see cref="SensePerceptionEvent{T}"/> so consumers can bind
    ///     it in the inspector (UnityEvent-based).</item>
    /// </list>
    /// </remarks>
    public class HearingSensor : MonoBehaviour, IAISense
    {
        [Min(0f)] public float hearingThreshold = 0.1f;

        [Tooltip("Optional. If set, sounds emitted by this transform (or its children) are ignored.")]
        [SerializeField] private Transform ignoreSource;

        [Header("Enabled")]
        [SerializeField] private bool senseEnabled = true;

        [Header("Gizmo")]
        [SerializeField] private float gizmoRadius = 10f;

        [Header("Events")]
        public SensePerceptionEvent<SoundEvent> OnHeard;

        public bool SenseEnabled { get => senseEnabled; set => senseEnabled = value; }

        private void OnEnable() => StimulusSystem<SoundEvent>.OnStimulus += HandleSound;
        private void OnDisable() => StimulusSystem<SoundEvent>.OnStimulus -= HandleSound;

        private void HandleSound(SoundEvent sound)
        {
            if (!senseEnabled) return;
            if (ignoreSource != null && sound.Source != null &&
                (sound.Source == ignoreSource || sound.Source.IsChildOf(ignoreSource))) return;

            float distance = Vector3.Distance(transform.position, sound.Position);
            if (distance > sound.Radius) return;

            float perceived = sound.Loudness * (1f - distance / sound.Radius);
            if (perceived < hearingThreshold) return;

            OnHeard?.Invoke(new SensePerception<SoundEvent>(sound, perceived));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = UnityEngine.Color.cyan;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        }
    }
}
