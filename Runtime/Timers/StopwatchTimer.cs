namespace JadedBelles.Util.Timers
{
    /// <summary>
    /// A <see cref="Timer"/> that counts up from zero. Never auto-stops — call
    /// <see cref="Timer.StopTimer"/> when the surrounding logic decides it's done, then read
    /// <see cref="GetTime"/> for the accumulated seconds.
    /// </summary>
    public class StopwatchTimer : Timer
    {
        public StopwatchTimer() : base(0) { }

        public override void Tick(float deltaTime)
        {
            if (isRunning)
            {
                Time += deltaTime;
            }
        }

        public override void Reset() => Time = 0;

        public float GetTime() => Time;
    }
}
