using System;
using UnityEngine;

namespace JadedBelles.Util.Input
{
    /// <summary>
    /// ScriptableObject-based input abstraction. Gameplay code depends on this asset rather
    /// than Unity's <c>InputSystem</c> directly, so binding changes and input remapping stay
    /// isolated to the InputReader subclass.
    /// </summary>
    /// <remarks>
    /// Consumers create a concrete subclass in their game project, e.g.:
    /// <code>
    /// [CreateAssetMenu(menuName = "Input/Player Input Reader")]
    /// public class PlayerInputReader : InputReader, PlayerInputActions.IPlayerActions
    /// {
    ///     private PlayerInputActions actions;
    ///
    ///     public override void Enable()
    ///     {
    ///         if (actions == null)
    ///         {
    ///             actions = new PlayerInputActions();
    ///             actions.Player.SetCallbacks(this);
    ///         }
    ///         actions.Player.Enable();
    ///     }
    ///
    ///     public override void Disable() =&gt; actions?.Player.Disable();
    ///
    ///     public void OnMove(InputAction.CallbackContext ctx) =&gt; RaiseMove(ctx.ReadValue&lt;Vector2&gt;());
    ///     // ...etc for the other callbacks
    /// }
    /// </code>
    /// The base intentionally does not bind to a specific <c>InputActionAsset</c> — subclasses
    /// own the generated actions wrapper and call the protected <c>Raise*</c> helpers to fan
    /// events out to gameplay code.
    /// </remarks>
    public abstract class InputReader : ScriptableObject
    {
        /// <summary>Fires with a 2D movement axis (usually stick / WASD). Zero-vector on release.</summary>
        public event Action<Vector2> MoveEvent;

        /// <summary>Fires when the primary action (click / tap / south button) is pressed.</summary>
        public event Action PrimaryPressed;

        /// <summary>Fires when the primary action is released.</summary>
        public event Action PrimaryReleased;

        /// <summary>Fires with the current pointer/screen position in pixels.</summary>
        public event Action<Vector2> PointerPositionChanged;

        /// <summary>Enable underlying input action maps. Override to wire up your generated actions wrapper.</summary>
        public virtual void Enable() { }

        /// <summary>Disable underlying input action maps. Override to tear down your generated actions wrapper.</summary>
        public virtual void Disable() { }

        /// <summary>Subclass hook that fans a movement value out to <see cref="MoveEvent"/> subscribers.</summary>
        protected void RaiseMove(Vector2 value) => MoveEvent?.Invoke(value);

        /// <summary>Subclass hook that fans a primary-press signal out to <see cref="PrimaryPressed"/> subscribers.</summary>
        protected void RaisePrimaryPressed() => PrimaryPressed?.Invoke();

        /// <summary>Subclass hook that fans a primary-release signal out to <see cref="PrimaryReleased"/> subscribers.</summary>
        protected void RaisePrimaryReleased() => PrimaryReleased?.Invoke();

        /// <summary>Subclass hook that fans a pointer-position update out to <see cref="PointerPositionChanged"/> subscribers.</summary>
        protected void RaisePointerPositionChanged(Vector2 value) => PointerPositionChanged?.Invoke(value);
    }
}
