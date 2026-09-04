using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace JadedBelles.Util.Coroutines
{
    /// <summary>
    /// Helpers for bridging Unity's coroutine world with .NET <see cref="Task"/>-based async APIs.
    /// The one-liner every Unity project ends up writing when it starts hitting HTTP or Cloud
    /// SDKs.
    /// </summary>
    public static class CoroutineUtils
    {
        /// <summary>
        /// Yields until <paramref name="task"/> completes. If the task faults, its exception is
        /// logged via <see cref="Debug.LogException(System.Exception)"/> — the coroutine still
        /// exits normally rather than throwing back into Unity's dispatcher, matching Unity's
        /// usual coroutine failure semantics.
        /// </summary>
        public static IEnumerator AwaitTask(Task task)
        {
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception);
            }
        }
    }
}
