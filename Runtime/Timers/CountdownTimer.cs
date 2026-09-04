namespace JadedBelles.Util.Timers
{
    /// <summary>
    /// A <see cref="Timer"/> that counts down from an initial duration to zero. Fires
    /// <c>OnTimerStop</c> once the remaining time hits zero. Reset restarts from the original
    /// duration, or a new duration if the overload is used.
    /// </summary>
    public class CountdownTimer : Timer
    {
        public CountdownTimer(float value) : base(value) { }

        public override void Tick(float deltaTime)
        {
            if (isRunning && Time > 0)
            {
                Time -= deltaTime;
            }

            if (isRunning && Time <= 0)
            {
                StopTimer();
            }
        }

        public bool IsFinished => Time <= 0;

        public override void Reset() => Time = initialTime;

        public void Reset(float newTime)
        {
            initialTime = newTime;
            Reset();
        }
    }
}
