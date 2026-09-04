using UnityEngine;

namespace JadedBelles.Util.Color
{
    /// <summary>
    /// The canonical JadedBelles brand color palette. Use these constants for status messages,
    /// UI accents, and any place brand colors need to stay in sync across titles.
    /// </summary>
    /// <remarks>
    /// Extracted from the shared <c>StroTheGoatUtils.ColorChanger</c> that had been copy-pasted
    /// verbatim across four repos (<c>StroTheGoatUtils</c>, <c>match3-repo</c>,
    /// <c>SyntyGameJam</c>, and <c>Anni-Gem-Speed-Trials</c>). Field names deliberately match
    /// the historical <c>ColorChanger</c> API so call sites migrate with a namespace swap.
    /// A legacy <c>ColorChanger</c> alias ships alongside this type — see
    /// <see cref="ColorChanger"/> — for zero-touch migration.
    /// </remarks>
    public static class Palette
    {
        /// <summary>Neutral / disabled grey. RGB(180, 180, 180).</summary>
        public static readonly UnityEngine.Color Grey = new UnityEngine.Color(180f / 255f, 180f / 255f, 180f / 255f, 1f);

        /// <summary>Success / affirmative green. RGB(84, 169, 4).</summary>
        public static readonly UnityEngine.Color Green = new UnityEngine.Color(84f / 255f, 169f / 255f, 4f / 255f, 1f);

        /// <summary>Informational cyan-blue. RGB(0, 204, 231).</summary>
        public static readonly UnityEngine.Color Blue = new UnityEngine.Color(0f / 255f, 204f / 255f, 231f / 255f, 1f);

        /// <summary>Premium / currency gold-orange. RGB(253, 127, 8).</summary>
        public static readonly UnityEngine.Color Gold = new UnityEngine.Color(253f / 255f, 127f / 255f, 8f / 255f, 1f);

        /// <summary>Highlight / legendary purple. RGB(107, 8, 255).</summary>
        public static readonly UnityEngine.Color Purple = new UnityEngine.Color(107f / 255f, 8f / 255f, 255f / 255f, 1f);

        /// <summary>Error / destructive red. RGB(255, 0, 0).</summary>
        public static readonly UnityEngine.Color Red = new UnityEngine.Color(255f / 255f, 0f / 255f, 0f / 255f, 1f);
    }
}
