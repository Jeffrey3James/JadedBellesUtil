using System;
using System.Collections;
using UnityEngine;

namespace JadedBelles.Util.Coroutines
{
    /// <summary>
    /// Small coroutine composition helpers for "wait, do a thing, then wait some more" patterns
    /// that show up all over gameplay and UI code.
    /// </summary>
    public static class WaitExtensions
    {
        /// <summary>
        /// Waits <paramref name="delayTime"/> seconds, invokes <paramref name="callback"/>, then
        /// waits <paramref name="continueTime"/> additional seconds before returning. Callback is
        /// null-safe.
        /// </summary>
        public static IEnumerator DelayerWithContinuance(float delayTime, float continueTime, Action callback)
        {
            yield return new WaitForSeconds(delayTime);
            callback?.Invoke();
            yield return new WaitForSeconds(continueTime);
        }
    }
}
