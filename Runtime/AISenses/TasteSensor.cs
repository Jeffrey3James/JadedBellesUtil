using System.Collections.Generic;
using UnityEngine;

namespace JadedBelles.Util.AISenses
{
    /// <summary>
    /// Taste sensor \u2014 fires when something is ingested by (or force-fed to) the character.
    /// </summary>
    /// <remarks>
    /// Unlike sound/scent, taste is usually delivered directly to one sensor rather than broadcast
    /// widely (a bait feeds ONE animal). Call <see cref="Feed"/> to deliver a taste directly; the
    /// component also subscribes to the global <see cref="StimulusSystem{TasteEvent}"/> for
    /// area-of-effect flavors (e.g. contaminated water in a lake) and honors the same
    /// <see cref="flavorTagFilter"/> for both paths.
    /// </remarks>
    public class TasteSensor : MonoBehaviour, IAISense
    {
        [Range(0f, 1f)] public float tasteThreshold = 0.05f;

        [Tooltip("If non-empty, only TasteEvents whose FlavorTag appears in this list will be considered.")]
        public List<string> flavorTagFilter = new();

        [Header("Area-of-effect radius")]
        [Tooltip("Max distance for globally-broadcast taste events (e.g. contaminated lake). Directly-fed tastes ignore this.")]
        [Min(0f)] public float aoeRadius = 3f;

        [Header("Enabled")]
        [SerializeField] private bool senseEnabled = true;

        [Header("Events")]
        public SensePerceptionEvent<TasteEvent> OnTasted;

        public bool SenseEnabled { get => senseEnabled; set => senseEnabled = value; }

        private void OnEnable() => StimulusSystem<TasteEvent>.OnStimulus += HandleGlobalTaste;
        private void OnDisable() => StimulusSystem<TasteEvent>.OnStimulus -= HandleGlobalTaste;

        /// <summary>Deliver a taste directly to this sensor \u2014 the canonical "the animal ate the bait" path.</summary>
        public void Feed(TasteEvent evt)
        {
            if (!senseEnabled) return;
            if (!PassesFilter(evt.FlavorTag)) return;
            if (evt.Potency < tasteThreshold) return;
            OnTasted?.Invoke(new SensePerception<TasteEvent>(evt, evt.Potency));
        }

        private void HandleGlobalTaste(TasteEvent evt)
        {
            if (!senseEnabled) return;
            if (!PassesFilter(evt.FlavorTag)) return;

            float distance = Vector3.Distance(transform.position, evt.Position);
            if (distance > aoeRadius) return;

            float falloff = aoeRadius > 0f ? 1f - distance / aoeRadius : 1f;
            float perceived = Mathf.Clamp01(evt.Potency * falloff);
            if (perceived < tasteThreshold) return;

            OnTasted?.Invoke(new SensePerception<TasteEvent>(evt, perceived));
        }

        private bool PassesFilter(string tag)
        {
            if (flavorTagFilter == null || flavorTagFilter.Count == 0) return true;
            return flavorTagFilter.Contains(tag);
        }
    }

    /// <summary>
    /// Trigger-collider bait that force-feeds a <see cref="TasteEvent"/> to any
    /// <see cref="TasteSensor"/> that enters, then optionally consumes itself.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BaitItem : MonoBehaviour
    {
        public string flavorTag = "bait";
        [Range(0f, 1f)] public float potency = 1f;
        public bool consumeOnEat = true;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var mouth = other.GetComponentInParent<TasteSensor>();
            if (mouth == null) return;

            mouth.Feed(new TasteEvent(transform.position, potency, flavorTag, transform));
            if (consumeOnEat) Destroy(gameObject);
        }
    }
}
