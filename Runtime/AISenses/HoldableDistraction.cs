using UnityEngine;

namespace JadedBelles.Util.AISenses
{
    /// <summary>
    /// Pickable world item that emits a <see cref="DistractionData"/> stimulus when dropped.
    /// A wine bottle, a rock, an alarm clock \u2014 anything a player can lift, carry, and set down
    /// somewhere loud/smelly enough to pull a guard off-post.
    /// </summary>
    /// <remarks>
    /// Extracted from SyntyGameJam's <c>HoldableItem</c>. Cleaned up:
    /// <list type="bullet">
    ///   <item>The <c>IHoldable</c>/<c>IInteractable</c> game-specific interfaces are gone \u2014
    ///     the package doesn't own an interaction system. Wire whatever pickup system you use to
    ///     <see cref="Pickup"/> and <see cref="Drop"/> directly.</item>
    ///   <item>The single <c>SoundSystem.Emit</c> call is replaced with the multi-sense
    ///     <see cref="DistractionData.EmitFrom"/> path, so one drop can ping sound + scent at once.</item>
    ///   <item>Kinematic-body / collider toggling remains \u2014 that's the practical part of the
    ///     pickup dance.</item>
    /// </list>
    /// </remarks>
    public class HoldableDistraction : MonoBehaviour
    {
        [SerializeField] private DistractionData data = new DistractionData();

        [Tooltip("If true, dropping the item also spawns a lingering ScentEmitter (when Scent sense is enabled).")]
        [SerializeField] private bool leaveScentTrail = false;

        private Rigidbody rb;
        private Collider col;

        public DistractionData Data => data;
        public bool IsHeld { get; private set; }

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
        }

        /// <summary>Parent to the hand, disable physics + collider so it rides along.</summary>
        public virtual void Pickup(Transform hand)
        {
            if (rb != null) rb.isKinematic = true;
            if (col != null) col.enabled = false;

            transform.SetParent(hand);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            IsHeld = true;
        }

        /// <summary>Detach, re-enable physics/collider, and broadcast the distraction from the current position.</summary>
        public virtual void Drop()
        {
            transform.parent = null;
            if (rb != null) rb.isKinematic = false;
            if (col != null) col.enabled = true;

            data.EmitFrom(transform.position, transform);
            if (leaveScentTrail) data.SpawnScentEmitter(transform.position);
            IsHeld = false;
        }
    }
}
