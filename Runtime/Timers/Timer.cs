using System;

namespace JadedBelles.Util.Timers
{
    /// <summary>
    /// Base class for tickable timers. Subclasses decide what "tick" means (count down, count up,
    /// etc.). The timer is plain C# — the caller is responsible for feeding it a delta-time each
    /// frame from a MonoBehaviour, so tests can drive it without Unity's game loop.
    /// </summary>
    public abstract class Timer
    {
        protected float initialTime;
        protected float Time { get; set; }
        public bool isRunning { get; protected set; }

        public float progress => Time / initialTime;

        public Action OnTimerStart = delegate { };
        public Action OnTimerStop = delegate { };
        public Action ForceTimerEnd = delegate { };

        protected Timer(float value)
        {
            initialTime = value;
            isRunning = false;
        }

        public void StartTimer()
        {
            Time = initialTime;
            if (!isRunning)
            {
                isRunning = true;
                OnTimerStart.Invoke();
            }
        }

        public void StopTimer()
        {
            if (isRunning)
            {
                isRunning = false;
                OnTimerStop.Invoke();
            }
        }

        public void ForceTimer()
        {
            if (isRunning)
            {
                isRunning = false;
            }
            ForceTimerEnd.Invoke();
        }

        public void Resume() => isRunning = true;
        public void Pause() => isRunning = false;
        public abstract void Reset();

        public abstract void Tick(float deltaTime);
    }
}
