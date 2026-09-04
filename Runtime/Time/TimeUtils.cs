using System;

namespace JadedBelles.Util.Time
{
    /// <summary>
    /// Provides utility functions for working with time, including Unix timestamps, countdown
    /// formatting, duration calculations, and safe parsing. Designed to be modular and reusable
    /// across multiple projects.
    /// </summary>
    public static class TimeUtils
    {
        /// <summary>
        /// Gets the current UTC time as a Unix timestamp (seconds since epoch).
        /// </summary>
        public static long UnixNow => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>
        /// Calculates the number of full minutes between two Unix timestamps.
        /// </summary>
        /// <param name="startUnix">The starting Unix time (in seconds).</param>
        /// <param name="endUnix">The ending Unix time (in seconds).</param>
        /// <returns>The number of minutes between the two timestamps.</returns>
        public static int MinutesBetween(long startUnix, long endUnix)
        {
            return (int)(endUnix - startUnix) / 60;
        }

        /// <summary>
        /// Returns a formatted countdown string showing the time left until a duration has passed
        /// from a start Unix timestamp.
        /// </summary>
        /// <param name="startUnixTime">The starting Unix timestamp (in seconds).</param>
        /// <param name="intervalDurationSeconds">The duration (in seconds) to count down from.</param>
        /// <param name="includeHours">If true, formats as HH:MM:SS. Otherwise MM:SS.</param>
        /// <returns>
        /// A countdown string like "12:34" or "01:12:34". Returns "00:00" if the duration has
        /// already passed.
        /// </returns>
        public static string FormatCountdown(long startUnixTime, int intervalDurationSeconds, bool includeHours = false)
        {
            long currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long secondsPassed = currentUnixTime - startUnixTime;
            long secondsRemaining = Math.Max(0, intervalDurationSeconds - secondsPassed);

            TimeSpan time = TimeSpan.FromSeconds(secondsRemaining);

            return includeHours
                ? $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}"
                : $"{time.Minutes:D2}:{time.Seconds:D2}";
        }

        /// <summary>
        /// Calculates the full <see cref="TimeSpan"/> between two Unix timestamps.
        /// </summary>
        /// <param name="startUnix">The start Unix timestamp (in seconds).</param>
        /// <param name="endUnix">The end Unix timestamp (in seconds).</param>
        /// <returns>A TimeSpan representing the duration between the two timestamps.</returns>
        public static TimeSpan DurationBetween(long startUnix, long endUnix)
        {
            return TimeSpan.FromSeconds(endUnix - startUnix);
        }

        /// <summary>
        /// Attempts to parse a string as a Unix timestamp. Returns 0 if parsing fails.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>The parsed Unix timestamp as a long, or 0 if invalid.</returns>
        public static long ParseUnixString(string s)
        {
            return long.TryParse(s, out long result) ? result : 0;
        }
    }

    /// <summary>
    /// A lightweight struct representing a snapshot of the current time, including both a
    /// <see cref="DateTime"/> and a Unix timestamp. Handy for logging or wire-serializing "when
    /// did this happen" in a form the server and client can both read.
    /// </summary>
    public struct TimeSnapshot
    {
        /// <summary>
        /// The current system time in UTC format.
        /// </summary>
        public DateTime CurrentTime;

        /// <summary>
        /// The current time as a Unix timestamp (seconds since epoch).
        /// </summary>
        public long CurrentUnixTime;

        /// <summary>
        /// Returns a formatted string containing both the UTC DateTime and Unix timestamp.
        /// </summary>
        /// <returns>A string representation of the time snapshot.</returns>
        public override string ToString()
        {
            return $"[Local UTC TIME: {CurrentTime}, Unix: {CurrentUnixTime} ]";
        }
    }
}
