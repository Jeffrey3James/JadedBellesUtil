using UnityEngine;

namespace JadedBelles.Util.StateMachine
{
    /// <summary>
    /// Thin MonoBehaviour wrapper that owns a <see cref="StateMachine"/> and drives its ticks
    /// from Unity's Update / FixedUpdate loops. Subclass and populate <see cref="Machine"/> with
    /// states and transitions in <see cref="Awake"/> or <see cref="Start"/>.
    /// </summary>
    public abstract class StateMachineComponent : MonoBehaviour
    {
        /// <summary>The underlying plain-C# state machine. Wire states + transitions here.</summary>
        public StateMachine Machine { get; } = new StateMachine();

        protected virtual void Update()
        {
            Machine.Tick();
        }

        protected virtual void FixedUpdate()
        {
            Machine.FixedTick();
        }
    }
}
