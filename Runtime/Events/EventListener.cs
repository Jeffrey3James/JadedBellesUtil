using UnityEngine;
using UnityEngine.Events;

namespace JadedBelles.Util.Events
{
    /// <summary>
    /// Generic MonoBehaviour listener that binds itself to an <see cref="EventChannel{T}"/> asset
    /// and forwards raised values to an inspector-configured <see cref="UnityEvent{T}"/>.
    /// </summary>
    /// <remarks>
    /// Drop this (or a concrete non-generic subclass) on a GameObject, drag the channel asset
    /// into the inspector slot, and wire the UnityEvent to the callbacks that should fire.
    /// </remarks>
    /// <typeparam name="T">Payload type; must match the paired <see cref="EventChannel{T}"/>.</typeparam>
    public abstract class EventListener<T> : MonoBehaviour
    {
        [SerializeField] private EventChannel<T> eventChannel;
        [SerializeField] private UnityEvent<T> unityEvent;

        protected virtual void Awake()
        {
            if (eventChannel == null)
            {
                return;
            }
            eventChannel.Register(this);
        }

        protected virtual void OnDestroy()
        {
            if (eventChannel != null)
            {
                eventChannel.Deregister(this);
            }
        }

        /// <summary>Invoked by the paired channel; forwards <paramref name="value"/> to the UnityEvent.</summary>
        public void Raise(T value)
        {
            unityEvent?.Invoke(value);
        }

        /// <summary>Returns the currently bound channel, or <c>null</c> if none is assigned.</summary>
        public EventChannel<T> GetEventChannel() => eventChannel;

        /// <summary>
        /// Rebinds this listener to a different channel at runtime, deregistering from the old
        /// channel and registering with the new one.
        /// </summary>
        public void SetEventChannel(EventChannel<T> channel)
        {
            if (eventChannel != null)
            {
                eventChannel.Deregister(this);
            }

            eventChannel = channel;

            if (eventChannel != null)
            {
                eventChannel.Register(this);
            }
        }
    }

    /// <summary>
    /// Convenience non-generic listener for signal-only <see cref="EventChannel"/> events.
    /// </summary>
    public class EventListener : EventListener<Empty> { }
}
