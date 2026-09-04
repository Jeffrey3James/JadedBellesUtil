#if JADEDBELLES_UITK
using UnityEngine.UIElements;

namespace JadedBelles.Util.UITK
{
    /// <summary>
    /// Fluent extension helpers on <see cref="VisualElement"/> for the UI Toolkit (UITK) —
    /// the kind of chainable "make a child, add classes, hang manipulators on it" one-liners
    /// that every UITK-using project rewrites eventually. Lifted verbatim from the canonical
    /// <c>StroTheGoatUtils.cs</c> copy so downstream games (Synty jam project, any future
    /// JadedBelles editor tools) can share one authoritative implementation.
    /// </summary>
    /// <remarks>
    /// This module is guarded by the <c>JADEDBELLES_UITK</c> version-define declared in the
    /// package asmdef, tied to <c>com.unity.modules.uielements</c>. The UI Elements module
    /// is a built-in Unity module that is auto-referenced on modern Unity, so the guard is
    /// belt-and-braces rather than strictly necessary — but it keeps the package honest on
    /// exotic project configurations that strip built-in modules.
    /// </remarks>
    public static class VisualElementsExtensions
    {
        /// <summary>
        /// Create a plain <see cref="VisualElement"/>, add the given USS classes to it, and
        /// attach it as a child of <paramref name="parent"/>. Returns the new child so it can
        /// be chained further.
        /// </summary>
        public static VisualElement CreateChild(this VisualElement parent, params string[] classes)
        {
            var child = new VisualElement();
            child.AddClass(classes).AddTo(parent);
            return child;
        }

        /// <summary>
        /// Create a new <typeparamref name="T"/>, add the given USS classes to it, and attach
        /// it as a child of <paramref name="parent"/>. Returns the new typed child so callers
        /// can keep chaining fluent calls against the concrete element type.
        /// </summary>
        public static T CreateChild<T>(this VisualElement parent, params string[] classes) where T : VisualElement, new()
        {
            var child = new T();
            child.AddClass(classes).AddTo(parent);
            return child;
        }

        /// <summary>
        /// Attach <paramref name="child"/> to <paramref name="parent"/> and return the child
        /// for further chaining.
        /// </summary>
        public static T AddTo<T>(this T child, VisualElement parent) where T : VisualElement, new()
        {
            parent.Add(child);
            return child;
        }

        /// <summary>
        /// Add every non-null / non-empty USS class in <paramref name="classes"/> to
        /// <paramref name="visualElement"/> and return the element for chaining.
        /// </summary>
        public static T AddClass<T>(this T visualElement, params string[] classes) where T : VisualElement
        {
            foreach (string cls in classes)
            {
                if (!string.IsNullOrEmpty(cls))
                {
                    visualElement.AddToClassList(cls);
                }
            }

            return visualElement;
        }

        /// <summary>
        /// Attach an <see cref="IManipulator"/> to <paramref name="visualElement"/> and return
        /// the element for chaining.
        /// </summary>
        public static T WithManipulators<T>(this T visualElement, IManipulator manipulator) where T : VisualElement
        {
            visualElement.AddManipulator(manipulator);
            return visualElement;
        }
    }
}
#endif
