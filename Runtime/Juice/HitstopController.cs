using System.Collections;
using UnityEngine;

namespace JadedBelles.Util.Juice
{
    /// <summary>
    /// "Freeze <see cref="UnityEngine.Time.timeScale"/> to 0 then restore" helper — the classic hitstop /
    /// impact-pause effect. Kept as a plain object (not a MonoBehaviour) so the caller owns the
    /// coroutine handle and cancel/extend semantics without adding another GameObject to the
    /// scene. Pass a <see cref="MonoBehaviour"/> host that will run the coroutine on your behalf.
    ///
    /// Overlapping calls: the longer window wins. A short 40ms request that arrives while a
    /// 200ms freeze is already in flight is ignored (returning early would risk restoring
    /// timeScale to 1 too soon). If the newer request is LONGER we extend the existing freeze.
    /// </summary>
    public class HitstopController
    {
        readonly MonoBehaviour _host;
        Coroutine _co;
        float _priorScale = 1f;
        float _resumeAtRealtime; // UnityEngine.Time.realtimeSinceStartup at which timeScale must be restored

        /// <summary>Create a controller that runs its coroutine on the given host MonoBehaviour.</summary>
        public HitstopController(MonoBehaviour host)
        {
            _host = host;
        }

        /// <summary>
        /// Begin (or extend) a hitstop for <paramref name="ms"/> milliseconds. If a freeze is
        /// already active the resume time is pushed out to whichever request lasts longer.
        /// </summary>
        public void Begin(int ms)
        {
            if (_host == null || ms <= 0) return;
            float dur = ms / 1000f;
            float desiredResume = UnityEngine.Time.realtimeSinceStartup + dur;

            if (_co != null)
            {
                // Already frozen — just push the resume-time out if the new window is longer.
                if (desiredResume > _resumeAtRealtime) _resumeAtRealtime = desiredResume;
                return;
            }

            _priorScale = UnityEngine.Time.timeScale;
            _resumeAtRealtime = desiredResume;
            _co = _host.StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            UnityEngine.Time.timeScale = 0f;
            // Loop so mid-freeze extensions work — always wait until the current resume target.
            while (UnityEngine.Time.realtimeSinceStartup < _resumeAtRealtime)
            {
                yield return null; // WaitForSecondsRealtime would ignore extensions
            }
            UnityEngine.Time.timeScale = _priorScale;
            _co = null;
        }
    }
}
