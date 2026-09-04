using UnityEngine;

namespace JadedBelles.Util.UnityExtensions
{
    /// <summary>
    /// Idiomatic Unity extension methods that get rewritten in every project eventually.
    /// Bundled together here so a single <c>using JadedBelles.Util.UnityExtensions;</c> unlocks
    /// both <see cref="GetOrAdd{T}(GameObject)"/> and <see cref="OrNull{T}(T)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>GetOrAdd&lt;T&gt;</c> is the canonical <c>StroTheGoatUtils</c> helper, extracted
    /// verbatim (three repos shipped it byte-identical). <c>OrNull&lt;T&gt;</c> is the fake-null
    /// bypass from the Synty fork — necessary because destroyed Unity objects compare
    /// <c>== null</c> yet are not actually a C# <c>null</c>, so plain <c>??</c> / <c>?.</c>
    /// operators misbehave without it.
    /// </para>
    /// </remarks>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Returns the existing <typeparamref name="T"/> component on <paramref name="gameObject"/>,
        /// or adds a fresh one and returns it if none is attached yet.
        /// </summary>
        /// <typeparam name="T">Component type to fetch or attach.</typeparam>
        public static T GetOrAdd<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        /// <summary>
        /// Bypasses Unity's fake-null so C# <c>??</c> and <c>?.</c> operators behave as expected
        /// on destroyed <see cref="Object"/> references. Returns the object when it is a live,
        /// non-destroyed Unity object; returns a real C# <c>null</c> otherwise.
        /// </summary>
        /// <typeparam name="T">Any subclass of <see cref="Object"/>.</typeparam>
        public static T OrNull<T>(this T obj) where T : Object => obj ? obj : null;
    }
}
