using UnityEditor;
using UnityEngine;

namespace JadedBelles.Util.EditorTools
{
    /// <summary>
    /// Small collection of <see cref="GUIStyle"/> and label helpers for custom inspectors,
    /// editor windows, and scene overlays. Lives in the <c>JadedBelles.Util.Editor</c>
    /// assembly so it never ships in player builds — every <c>.cs</c> file under
    /// <c>Editor/</c> is Editor-only automatically because the asmdef declares
    /// <c>"includePlatforms": ["Editor"]</c>.
    /// </summary>
    /// <remarks>
    /// This was extracted from the four-copy <c>StroTheGoatUtils.EditorUtils</c> block. Only
    /// the Match3 copy remembered to wrap the class in <c>#if UNITY_EDITOR</c>; the canonical
    /// and Synty copies leaked <c>UnityEditor</c> into player builds. Landing this in a
    /// platform-filtered assembly makes the fix universal — consumers no longer need the
    /// <c>#if</c> guard because the assembly itself is stripped from non-editor targets.
    /// </remarks>
    public static class EditorUtils
    {
        /// <summary>
        /// Render a <see cref="GUILayout.Label(string, GUIStyle, GUILayoutOption[])"/> using
        /// a temporary clone of <paramref name="guiStyle"/> with its normal text color
        /// overridden. Cloning avoids mutating the caller's shared style asset.
        /// </summary>
        public static void CreateLabelAndConfigure(string labelName, GUIStyle guiStyle, Color color)
        {
            GUIStyle tempStyle = new GUIStyle(guiStyle);
            tempStyle.normal.textColor = color;
            GUILayout.Label(labelName, tempStyle);
        }

        /// <summary>
        /// Build a bold, center-aligned <see cref="GUIStyle"/> at the requested font size on
        /// top of <see cref="EditorStyles.boldLabel"/>. Handy for section headers inside
        /// custom editor windows.
        /// </summary>
        public static GUIStyle CenteredStyle(int fontSize)
        {
            GUIStyle centeredStyle = new GUIStyle(EditorStyles.boldLabel);
            centeredStyle.alignment = TextAnchor.MiddleCenter;
            centeredStyle.fontSize = fontSize;
            return centeredStyle;
        }

        /// <summary>
        /// Shorthand for <see cref="GUILayout.Space(float)"/> that reads more clearly at the
        /// call site (<c>AddSpaceToGUI(12)</c> vs <c>GUILayout.Space(12)</c>) inside long
        /// custom-editor layouts.
        /// </summary>
        public static void AddSpaceToGUI(int spaceToAdd)
        {
            GUILayout.Space(spaceToAdd);
        }
    }
}
