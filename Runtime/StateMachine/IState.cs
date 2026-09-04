namespace JadedBelles.Util.StateMachine
{
    /// <summary>
    /// Contract for a single state driven by <see cref="StateMachine"/>. All hooks are optional —
    /// implement only the ones you need and leave the rest as empty method bodies.
    /// </summary>
    /// <remarks>
    /// The state machine is intentionally Unity-free: <see cref="Update"/> and
    /// <see cref="FixedUpdate"/> take no arguments and are driven by the caller (typically a
    /// <see cref="StateMachineComponent"/>). Keep state classes lightweight and side-effect free
    /// outside these hooks so they're easy to unit-test.
    /// </remarks>
    public interface IState
    {
        /// <summary>Called once when the state machine transitions into this state.</summary>
        void OnEnter();

        /// <summary>Called every frame while this state is active. Drive from <c>MonoBehaviour.Update</c>.</summary>
        void Update();

        /// <summary>Called every physics step while this state is active. Drive from <c>MonoBehaviour.FixedUpdate</c>.</summary>
        void FixedUpdate();

        /// <summary>Called once when the state machine transitions out of this state.</summary>
        void OnExit();
    }
}
