#if JADEDBELLES_UGS_CORE && JADEDBELLES_UGS_AUTH
using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace JadedBelles.Util.Services
{
    /// <summary>
    /// One-shot helper that wraps Unity Gaming Services (UGS) initialization plus the
    /// standard "sign in anonymously if not already signed in" fallback. Every JadedBelles
    /// title that talks to UGS (Leaderboards, Cloud Save, Relay, Authentication) runs the
    /// same fifteen-line try/await/log block against <see cref="UnityServices.InitializeAsync"/>
    /// and <see cref="AuthenticationService.SignInAnonymouslyAsync"/>; this type consolidates
    /// that dance and exposes both a Task-based and coroutine-friendly entry point.
    /// </summary>
    /// <remarks>
    /// This module is guarded by the <c>JADEDBELLES_UGS_CORE</c> and <c>JADEDBELLES_UGS_AUTH</c>
    /// version-defines declared in the package asmdef, so the file compiles away cleanly on
    /// projects that do not have the UGS Core (<c>com.unity.services.core</c>) and
    /// Authentication (<c>com.unity.services.authentication</c>) packages installed.
    /// </remarks>
    public static class UnityServicesInitializer
    {
        /// <summary>
        /// True once <see cref="InitializeAndSignInAsync"/> has run to completion at least
        /// once in the current process. Set only on the successful path; a caught exception
        /// leaves this <c>false</c> so the next call retries.
        /// </summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// Convenience passthrough to <see cref="AuthenticationService.IsSignedIn"/>. Safe to
        /// read after a successful <see cref="InitializeAndSignInAsync"/>; will throw the
        /// usual UGS "services not initialized" exception if read too early.
        /// </summary>
        public static bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;

        /// <summary>
        /// Initialize UGS Core and, if the player is not already signed in, sign them in
        /// anonymously. Idempotent: returns immediately once <see cref="IsInitialized"/> is
        /// true and the player is still signed in. Exceptions are caught and logged with
        /// <see cref="Debug.LogError(object)"/> rather than propagated, matching the pre-
        /// extraction behavior of the four sibling repos this was consolidated from.
        /// </summary>
        public static async Task InitializeAndSignInAsync()
        {
            if (IsInitialized && AuthenticationService.Instance.IsSignedIn)
                return;

            try
            {
                await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log("Signed in anonymously as: " + AuthenticationService.Instance.PlayerId);
                }

                IsInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError("Unity Services Init Failed: " + e.Message);
            }
        }

        /// <summary>
        /// Coroutine-friendly wrapper around <see cref="InitializeAndSignInAsync"/>. Yields
        /// until the underlying <see cref="Task"/> completes, so callers can
        /// <c>yield return UnityServicesInitializer.InitializeAndSignIn();</c> from any
        /// MonoBehaviour coroutine without pulling in an async/await bridge.
        /// </summary>
        public static IEnumerator InitializeAndSignIn()
        {
            Task task = InitializeAndSignInAsync();
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
                Debug.LogException(task.Exception);
        }
    }
}
#endif
