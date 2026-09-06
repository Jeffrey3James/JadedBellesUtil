using System.Collections.Generic;
using UnityEngine;

namespace JadedBelles.Util.AISenses
{
    /// <summary>
    /// Event-driven smell sensor. Subscribes to <see cref="StimulusSystem{ScentEvent}"/>; each
    /// received pulse is scaled by distance falloff and, if configured, an alignment bias against
    /// the sensor's <see cref="windDirection"/> so downwind scents register stronger than upwind.
    /// Fires <see cref="OnSmelled"/> when perceived strength beats <see cref="smellThreshold"/>.
    /// </summary>
    /// <remarks>
    /// The wind bias is a simple dot-product between the wind direction and the source-to-sensor
    /// vector: fully downwind = 1 + <see cref="windAdvantage"/>, fully upwind = 1 - <see cref="windAdvantage"/>.
    /// Set <see cref="windAdvantage"/> to 0 to ignore wind. The optional <see cref="scentTagFilter"/>
    /// list lets the sensor react only to specific scent kinds (dogs smell blood, not perfume).
    /// </remarks>
    public class SmellSensor : MonoBehaviour, IAISense
    {
        [Header("Sensitivity")]
        [Range(0f, 1f)] public float smellThreshold = 0.05f;

        [Tooltip("If non-empty, only ScentEvents whose ScentTag appears in this list will be considered.")]
        public List<string> scentTagFilter = new();

        [Header("Wind")]
        [Tooltip("World-space wind direction; the sensor smells strongest when the source is upwind of it.")]
        public Vector3 windDirection = Vector3.forward;

        [Tooltip("How much wind can amplify or attenuate strength, 0..1. 0 = ignore wind, 1 = fully upwind doubles, fully downwind kills it.")]
        [Range(0f, 1f)] public float windAdvantage = 0.5f;

        [Header("Enabled")]
        [SerializeField] private bool senseEnabled = true;

        [Header("Events")]
        public SensePerceptionEvent<ScentEvent> OnSmelled;

        public bool SenseEnabled { get => senseEnabled; set => senseEnabled = value; }

        private void OnEnable() => StimulusSystem<ScentEvent>.OnStimulus += HandleScent;
        private void OnDisable() => StimulusSystem<ScentEvent>.OnStimulus -= HandleScent;

        private void HandleScent(ScentEvent scent)
        {
            if (!senseEnabled) return;
            if (scentTagFilter != null && scentTagFilter.Count > 0 && !scentTagFilter.Contains(scent.ScentTag)) return;

            float distance = Vector3.Distance(transform.position, scent.Position);
            if (distance > scent.Radius) return;

            float falloff = 1f - distance / scent.Radius;
            float perceived = scent.Strength * falloff;

            if (windAdvantage > 0f && windDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 sourceToSensor = (transform.position - scent.Position).normalized;
                float alignment = Vector3.Dot(windDirection.normalized, sourceToSensor);
                // alignment = +1 when the sensor is DOWNWIND of the source (wind carries the scent to us).
                perceived *= Mathf.Clamp(1f + alignment * windAdvantage, 0f, 2f);
            }

            perceived = Mathf.Clamp01(perceived);
            if (perceived < smellThreshold) return;

            OnSmelled?.Invoke(new SensePerception<ScentEvent>(scent, perceived));
        }
    }
}
