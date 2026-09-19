using UnityEngine;
using UnityEngine.Events;

namespace JadedBelles.Util.AISenses
{
    /// <summary>
    /// Polling line-of-sight sensor: does the target sit inside the view cone (distance + angle)
    /// and does an obstacle-mask raycast between eye and target return clear? Fires
    /// <see cref="OnSpotted"/> on each Update tick the target is visible.
    /// </summary>
    /// <remarks>
    /// Extracted from SyntyGameJam's <c>VisionSettings</c>. Cleaned up:
    /// <list type="bullet">
    ///   <item>The hardcoded <c>GameEventsManager.instance.gameEvents.GameStop()</c> side effect is gone;
    ///     reactions belong on the consumer side of <see cref="OnSpotted"/>.</item>
    ///   <item>The always-on <c>Debug.Log("Player Seen")</c> is gone.</item>
    ///   <item>Renamed <c>CanSeePlayer</c> to <c>CanSeeTarget</c>; the sensor doesn't know or care
    ///     what the target is.</item>
    ///   <item>Now implements <see cref="IAISense"/> and honors <see cref="SenseEnabled"/>.</item>
    /// </list>
    /// </remarks>
    public class SightSensor : MonoBehaviour, IAISense
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Cone")]
        [Min(0f)] public float viewDistance = 10f;
        [Range(0f, 360f)] public float viewAngle = 90f;
        public LayerMask obstacleMask;

        [Header("Eye positions")]
        [SerializeField] private float eyeHeight = 1.5f;
        [SerializeField] private float targetEyeHeight = 1.6f;

        [Header("Enabled")]
        [SerializeField] private bool senseEnabled = true;

        [Header("Events")]
        public UnityEvent<Transform> OnSpotted;
        public UnityEvent<Transform> OnLost;

        private bool wasVisibleLastFrame;

        public bool SenseEnabled { get => senseEnabled; set => senseEnabled = value; }
        public Transform Target { get => target; set => target = value; }
        private Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;
        private Vector3 TargetPosition => target.position + Vector3.up * targetEyeHeight;

        private void Update()
        {
            bool visible = senseEnabled && CanSeeTarget();

            if (visible && !wasVisibleLastFrame) OnSpotted?.Invoke(target);
            if (!visible && wasVisibleLastFrame) OnLost?.Invoke(target);

            wasVisibleLastFrame = visible;
        }

        /// <summary>True when the target is within range, inside the view cone, and unobstructed.</summary>
        public bool CanSeeTarget()
        {
            if (target == null) return false;
            if (!IsInRange()) return false;
            if (!IsInViewCone()) return false;
            if (!HasLineOfSight()) return false;
            return true;
        }

        private bool IsInRange() => Vector3.Distance(EyePosition, TargetPosition) <= viewDistance;

        private bool IsInViewCone()
        {
            Vector3 direction = (TargetPosition - EyePosition).normalized;
            float angle = Vector3.Angle(transform.forward, direction);
            return angle <= viewAngle * 0.5f;
        }

        private bool HasLineOfSight()
        {
            Vector3 direction = TargetPosition - EyePosition;
            return !Physics.Raycast(EyePosition, direction.normalized, direction.magnitude, obstacleMask);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = UnityEngine.Color.yellow;
            Gizmos.DrawWireSphere(EyePosition, viewDistance);

            Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
            Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;
            Gizmos.color = UnityEngine.Color.cyan;
            Gizmos.DrawLine(EyePosition, EyePosition + left * viewDistance);
            Gizmos.DrawLine(EyePosition, EyePosition + right * viewDistance);

            Gizmos.color = UnityEngine.Color.blue;
            Gizmos.DrawSphere(EyePosition, 0.08f);

            if (target != null)
            {
                Gizmos.color = HasLineOfSight() ? UnityEngine.Color.green : UnityEngine.Color.red;
                Gizmos.DrawLine(EyePosition, TargetPosition);
                Gizmos.DrawSphere(TargetPosition, 0.08f);
            }
        }
    }
}
