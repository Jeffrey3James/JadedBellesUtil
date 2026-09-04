using System;
using System.Collections.Generic;
using UnityEngine;

namespace JadedBelles.Util.Pooling
{
    /// <summary>
    /// Small hand-rolled component pool that avoids <c>Instantiate</c> / <c>Destroy</c> churn
    /// for hot-loop objects (projectiles, hit VFX, collectable pickups, one-shot audio sources).
    /// </summary>
    /// <remarks>
    /// This is intentionally distinct from Unity's built-in <c>UnityEngine.Pool.ObjectPool&lt;T&gt;</c>:
    /// the pool operates on <see cref="Component"/> subclasses, calls <c>SetActive</c> on the
    /// owning <see cref="GameObject"/> around <see cref="Get"/> / <see cref="Release"/>, and
    /// silently ignores double-release attempts. Callers supply a factory closure so the pool
    /// stays agnostic of how instances are produced (prefab instantiation, <c>new</c>-up on a
    /// fresh GameObject, etc.).
    /// </remarks>
    /// <typeparam name="T">The pooled component type. Must derive from <see cref="Component"/>.</typeparam>
    public sealed class ObjectPool<T> where T : Component
    {
        private readonly Stack<T> _available = new Stack<T>();
        private readonly Func<T> _factory;

        /// <summary>Number of pooled instances currently sitting idle in the free list.</summary>
        public int AvailableCount => _available.Count;

        /// <summary>
        /// Creates a pool that produces new instances by invoking <paramref name="factory"/>
        /// when the free list is empty.
        /// </summary>
        /// <param name="factory">
        /// Delegate that returns a fresh, ready-to-use instance. Typically wraps
        /// <c>Object.Instantiate(prefab, parent)</c>.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is null.</exception>
        public ObjectPool(Func<T> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Returns a live instance — either popped from the free list or freshly minted via the
        /// factory. The returned instance's <see cref="GameObject"/> is set active before return.
        /// </summary>
        public T Get()
        {
            T item = _available.Count > 0 ? _available.Pop() : _factory();
            item.gameObject.SetActive(true);
            return item;
        }

        /// <summary>
        /// Returns <paramref name="item"/> to the free list and deactivates its
        /// <see cref="GameObject"/>. Null or already-pooled items are ignored.
        /// </summary>
        public void Release(T item)
        {
            if (item == null || _available.Contains(item))
                return;

            item.gameObject.SetActive(false);
            _available.Push(item);
        }
    }
}
