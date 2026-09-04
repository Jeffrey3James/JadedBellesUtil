using System.Collections.Generic;
using UnityEngine;

namespace JadedBelles.Util.Events
{
    /// <summary>
    /// A ScriptableObject-based event channel. Producers reference the channel asset and call
    /// <see cref="Invoke"/>; consumers register an <see cref="EventListener{T}"/> (or a plain
    /// listener via <see cref="Register"/>) to react.
    /// </summary>
    /// <remarks>
    /// Extracted from Match3's <c>Assets/_Scripts/EventChannel/</c>. The generic base is
    /// intentionally abstract — ship concrete channels (e.g. <c>BoolEventChannel</c>,
    /// <c>ScoreChangedEventChannel</c>) in your game project with a <c>[CreateAssetMenu]</c>
    /// attribute so they show up in the Unity asset menu.
    /// </remarks>
    /// <typeparam name="T">Payload type dispatched with every <see cref="Invoke"/> call.</typeparam>
    public abstract class EventChannel<T> : ScriptableObject
    {
        private readonly HashSet<EventListener<T>> observers = new HashSet<EventListener<T>>();

        /// <summary>
        /// Fires the channel, forwarding <paramref name="value"/> to every registered listener.
        /// </summary>
        public void Invoke(T value)
        {
            foreach (var observer in observers)
            {
                observer.Raise(value);
            }
        }

        /// <summary>Adds a listener to the channel's observer set. Idempotent.</summary>
        public void Register(EventListener<T> observer) => observers.Add(observer);

        /// <summary>Removes a listener from the channel's observer set. Safe to call twice.</summary>
        public void Deregister(EventListener<T> observer) => observers.Remove(observer);
    }

    /// <summary>
    /// No-payload variant of <see cref="EventChannel{T}"/> for pure-signal events (e.g. "level
    /// completed", "player died"). Backed by the empty <see cref="Empty"/> struct so the same
    /// generic listener machinery works.
    /// </summary>
    public abstract class EventChannel : EventChannel<Empty>
    {
        /// <summary>Fires the channel with the sentinel <see cref="Empty"/> payload.</summary>
        public void Invoke() => Invoke(default);
    }

    /// <summary>
    /// Zero-field sentinel payload used by <see cref="EventChannel"/> and any callers that want
    /// a signal-only event without inventing a bespoke type.
    /// </summary>
    public readonly struct Empty { }
}
