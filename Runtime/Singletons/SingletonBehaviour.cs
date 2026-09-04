using UnityEngine;

namespace JadedBelles.Util.Singletons
{
    /// <summary>
    /// Base class for MonoBehaviours that should exist as a single scene-persistent instance.
    /// Consolidates the "if (Instance == null) { Instance = this; DontDestroyOnLoad; } else { Destroy; }"
    /// Awake dance that gets copy-pasted across managers in every Unity project.
    ///
    /// Subclasses opt out of <c>DontDestroyOnLoad</c> by overriding <see cref="PersistAcrossScenes"/>.
    /// Subclasses that need Awake-time setup should override <see cref="OnSingletonAwake"/> — that
    /// callback is only invoked on the surviving instance, so per-instance initialization never
    /// runs on the duplicate that's about to be destroyed.
    /// </summary>
    /// <typeparam name="T">
    /// The concrete subclass. Pass the subclass itself as the type argument, curiously-recurring-
    /// template-pattern style: <c>public class AudioManager : SingletonBehaviour&lt;AudioManager&gt;</c>.
    /// </typeparam>
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
    {
        /// <summary>The single live instance, or null when no instance exists yet.</summary>
        public static T Instance { get; private set; }

        /// <summary>
        /// When true (the default), the singleton is marked <c>DontDestroyOnLoad</c> so it persists
        /// across scene changes. Override and return <c>false</c> for per-scene singletons
        /// (e.g. a per-level manager that should be re-authored each scene).
        /// </summary>
        protected virtual bool PersistAcrossScenes => true;

        /// <summary>
        /// Handles the singleton assignment. If another instance already exists, the duplicate
        /// destroys its own GameObject and returns before <see cref="OnSingletonAwake"/> runs.
        /// Subclasses should override <see cref="OnSingletonAwake"/> instead of <c>Awake</c>.
        /// </summary>
        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;
            if (PersistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
            OnSingletonAwake();
        }

        /// <summary>
        /// Called once, on the surviving instance, after the singleton reference has been set and
        /// (if requested) <c>DontDestroyOnLoad</c> has been applied. Override for one-time setup
        /// that used to live in <c>Awake</c>.
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        /// <summary>
        /// Clears the static instance reference when the surviving instance is destroyed.
        /// Duplicates never reach here because they short-circuit in <see cref="Awake"/>.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
