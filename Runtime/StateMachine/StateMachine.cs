using System;
using System.Collections.Generic;

namespace JadedBelles.Util.StateMachine
{
    /// <summary>
    /// A lightweight, transition-based finite state machine. Register per-state transitions with
    /// <see cref="AddTransition"/> and global transitions with <see cref="AddAnyTransition"/>,
    /// then drive the machine each frame via <see cref="Tick"/> / <see cref="FixedTick"/>.
    /// </summary>
    /// <remarks>
    /// The machine has no Unity dependency so it can be unit-tested in isolation. For the common
    /// case of driving from a MonoBehaviour, wrap it with <see cref="StateMachineComponent"/>.
    /// Transitions are evaluated in insertion order; the first satisfied predicate wins.
    /// "Any" transitions are evaluated before per-state transitions.
    /// </remarks>
    public class StateMachine
    {
        /// <summary>Fires after the machine finishes a transition into a new state.</summary>
        public event Action<IState> OnStateChanged;

        private IState currentState;

        private readonly Dictionary<IState, List<Transition>> transitions = new Dictionary<IState, List<Transition>>();
        private readonly List<Transition> anyTransitions = new List<Transition>();
        private static readonly List<Transition> EmptyTransitions = new List<Transition>(0);

        private List<Transition> currentTransitions = EmptyTransitions;

        /// <summary>The state the machine is currently in, or <c>null</c> before the first <see cref="SetState"/>.</summary>
        public IState CurrentState => currentState;

        /// <summary>Force-transitions the machine into <paramref name="state"/>, calling exit/enter hooks.</summary>
        public void SetState(IState state)
        {
            if (state == currentState)
            {
                return;
            }

            currentState?.OnExit();
            currentState = state;

            if (!transitions.TryGetValue(currentState, out currentTransitions))
            {
                currentTransitions = EmptyTransitions;
            }

            currentState?.OnEnter();
            OnStateChanged?.Invoke(currentState);
        }

        /// <summary>Registers a transition from <paramref name="from"/> to <paramref name="to"/> when <paramref name="condition"/> returns true.</summary>
        public void AddTransition(IState from, IState to, Func<bool> condition)
        {
            if (!transitions.TryGetValue(from, out var list))
            {
                list = new List<Transition>();
                transitions[from] = list;
            }
            list.Add(new Transition(to, condition));
        }

        /// <summary>Registers a transition from any state to <paramref name="to"/> when <paramref name="condition"/> returns true.</summary>
        public void AddAnyTransition(IState to, Func<bool> condition)
        {
            anyTransitions.Add(new Transition(to, condition));
        }

        /// <summary>Evaluates transitions and ticks the active state. Call once per <c>Update</c>.</summary>
        public void Tick()
        {
            var next = GetTriggeredTransition();
            if (next != null)
            {
                SetState(next.To);
            }

            currentState?.Update();
        }

        /// <summary>Ticks the active state's <c>FixedUpdate</c>. Call once per <c>FixedUpdate</c>.</summary>
        public void FixedTick()
        {
            currentState?.FixedUpdate();
        }

        private Transition GetTriggeredTransition()
        {
            for (int i = 0; i < anyTransitions.Count; i++)
            {
                if (anyTransitions[i].Condition())
                {
                    return anyTransitions[i];
                }
            }

            for (int i = 0; i < currentTransitions.Count; i++)
            {
                if (currentTransitions[i].Condition())
                {
                    return currentTransitions[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Internal transition record — pairs a destination state with the predicate that fires it.
        /// </summary>
        private class Transition
        {
            public readonly IState To;
            public readonly Func<bool> Condition;

            public Transition(IState to, Func<bool> condition)
            {
                To = to;
                Condition = condition;
            }
        }
    }
}
