using UnityEditor;
using UnityEditor.Overlays;

namespace JadedBelles.Util.EditorTools
{
    /// <summary>
    /// Scene View overlay that hosts the <see cref="SceneSwitcherToolbar"/> dropdown. Enable it
    /// from the Scene View's Overlays menu (top-left "…" button) as <c>Scene Switcher</c>.
    /// </summary>
    [Overlay(typeof(SceneView), "Scene Switcher")]
    public class SceneSwitcherOverlay : ToolbarOverlay
    {
        public SceneSwitcherOverlay() : base(SceneSwitcherToolbar.ID) { }
    }
}
