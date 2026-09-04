using UnityEngine;
using UnityEngine.EventSystems;

namespace JadedBelles.Util.Input
{
    /// <summary>
    /// On-screen virtual joystick for mobile builds. Attach to a Canvas element that has a
    /// background <see cref="RectTransform"/> and a knob <see cref="RectTransform"/>; the knob
    /// is clamped to the background's <see cref="radius"/> during drag, and <see cref="Value"/>
    /// exposes the current stick position as a <see cref="Vector2"/> in the <c>[-1, 1]</c> range.
    /// </summary>
    /// <remarks>
    /// Requires an <see cref="EventSystem"/> in the scene so Unity's pointer callbacks are
    /// dispatched. Pair with a <c>GraphicRaycaster</c> on the parent Canvas so touches actually
    /// route to this widget.
    /// </remarks>
    public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform knob;

        [Header("Feel")]
        [SerializeField, Min(10f)] private float radius = 100f;
        [SerializeField, Range(0f, 1f)] private float snapBack = 1f;

        /// <summary>Current stick value in <c>[-1, 1]</c> on each axis. Reset to zero on pointer-up.</summary>
        public Vector2 Value { get; private set; }

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateKnob(eventData);
        }

        /// <inheritdoc />
        public void OnDrag(PointerEventData eventData)
        {
            UpdateKnob(eventData);
        }

        /// <inheritdoc />
        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;
            if (knob != null)
                knob.anchoredPosition = Vector2.Lerp(knob.anchoredPosition, Vector2.zero, snapBack);
        }

        private void UpdateKnob(PointerEventData eventData)
        {
            if (background == null || knob == null)
                return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out localPoint);
            Vector2 clamped = Vector2.ClampMagnitude(localPoint, radius);
            knob.anchoredPosition = clamped;
            Value = clamped / radius;
        }
    }
}
