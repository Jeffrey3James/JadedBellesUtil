using UnityEngine;

namespace JadedBelles.Util.AISenses
{
    /// <summary>
    /// Pickable world item that emits a <see cref="DistractionData"/> stimulus both when it's
    /// thrown AND when it lands on any collider on <see cref="impactLayers"/>. A bottle to lob
    /// past a guard, a coin to throw down a hallway.
    /// </summary>
    /// <remarks>
    /// Extracted from SyntyGameJam's <c>ThrowableItem</c>. Cleaned up:
    /// <list type="bullet">
    ///   <item>The interaction-system interfaces are gone; wire your own pickup system to
    ///     <see cref="Pickup"/>/<see cref="Drop"/>/<see cref="Throw"/>.</item>
    ///   <item>The single-int <c>obstacleLayer</c> is now a proper <see cref="LayerMask"/> named
    ///     <see cref="impactLayers"/> so multiple layers (wall + floor + prop) can each register hits.</item>
    ///   <item>Multi-sense broadcast via <see cref="DistractionData.EmitFrom"/> instead of a
    ///     single sound event.</item>
    ///   <item>New <see cref="oneShot"/> toggle prevents rapid re-triggers when a bottle bounces.</item>
    /// </list>
    /// </remarks>
    public class ThrowableDistraction : HoldableDistraction
    {
        [Header("Impact")]
        [Tooltip("Distraction fires on collision with any collider on these layers.")]
        [SerializeField] private LayerMask impactLayers = ~0;

        [Tooltip("When true, only the first impact after a throw fires the distraction. Prevents bounces from re-triggering.")]
        [SerializeField] private bool oneShot = true;

        [Tooltip("Seconds after Throw before impact detection begins. Prevents self-hit at release.")]
        [Min(0f)][SerializeField] private float impactArmDelay = 0.05f;

        private float impactArmedAt;
        private bool hasImpactedThisThrow;

        /// <summary>Detach from the hand, apply an impulse, broadcast the distraction from the throw point.</summary>
        public virtual void Throw(Vector3 direction, float force)
        {
            transform.SetParent(null);

            var rb = GetComponent<Rigidbody>();
            var col = GetComponent<Collider>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.AddForce(direction.normalized * force, ForceMode.Impulse);
            }
            if (col != null) col.enabled = true;

            impactArmedAt = UnityEngine.Time.time + impactArmDelay;
            hasImpactedThisThrow = false;

            Data.EmitFrom(transform.position, transform);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (UnityEngine.Time.time < impactArmedAt) return;
            if (oneShot && hasImpactedThisThrow) return;
            if ((impactLayers.value & (1 << collision.gameObject.layer)) == 0) return;

            Data.EmitFrom(transform.position, transform);
            hasImpactedThisThrow = true;
        }
    }
}
