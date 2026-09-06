using System;

namespace JadedBelles.Util.AISenses
{
    /// <summary>
    /// Generic global pubsub bus for a single stimulus type. Emitters call <see cref="Emit"/>, every
    /// subscribed sensor gets the raw event and decides for itself whether to react.
    /// </summary>
    /// <remarks>
    /// One static bus per <typeparamref name="TEvent"/> keeps subscription simple and allocation-free.
    /// The <see cref="Sound"/>, <see cref="Scent"/>, and <see cref="Touch"/> aliases below are the
    /// canonical buses for the sound / smell / touch senses; games can also declare their own bus
    /// for custom stimuli (e.g. <c>StimulusSystem&lt;MagicPingEvent&gt;.Emit(...)</c>) without
    /// modifying the package.
    /// </remarks>
    public static class StimulusSystem<TEvent>
    {
        /// <summary>Fires every time an emitter publishes. Never null-checked here \u2014 sensors self-subscribe.</summary>
        public static event Action<TEvent> OnStimulus;

        /// <summary>Broadcast a stimulus to every subscribed sensor.</summary>
        public static void Emit(TEvent stimulus)
        {
            OnStimulus?.Invoke(stimulus);
        }

        /// <summary>Remove every subscriber. Useful for scene teardown in tests.</summary>
        public static void ClearAllListeners()
        {
            OnStimulus = null;
        }
    }

    /// <summary>Convenience non-generic access points for the canonical stimulus buses.</summary>
    public static class Stimuli
    {
        public static void EmitSound(SoundEvent e) => StimulusSystem<SoundEvent>.Emit(e);
        public static void EmitScent(ScentEvent e) => StimulusSystem<ScentEvent>.Emit(e);
        public static void EmitTouch(TouchEvent e) => StimulusSystem<TouchEvent>.Emit(e);
        public static void EmitTaste(TasteEvent e) => StimulusSystem<TasteEvent>.Emit(e);
    }
}
