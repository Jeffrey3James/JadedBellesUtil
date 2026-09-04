namespace JadedBelles.Util.Color
{
    /// <summary>
    /// Legacy alias for <see cref="Palette"/>. Preserved so existing call sites in the
    /// pre-extraction codebases (<c>StroAuthentication.LoginMessage.color = ColorChanger.Green;</c>
    /// and similar) migrate to the util package with only a namespace change.
    /// </summary>
    /// <remarks>
    /// New code should reference <see cref="Palette"/> directly. This type intentionally mirrors
    /// the field surface of <see cref="Palette"/> one-for-one and forwards each value.
    /// </remarks>
    public static class ColorChanger
    {
        /// <summary>Alias of <see cref="Palette.Grey"/>.</summary>
        public static readonly UnityEngine.Color Grey = Palette.Grey;

        /// <summary>Alias of <see cref="Palette.Green"/>.</summary>
        public static readonly UnityEngine.Color Green = Palette.Green;

        /// <summary>Alias of <see cref="Palette.Blue"/>.</summary>
        public static readonly UnityEngine.Color Blue = Palette.Blue;

        /// <summary>Alias of <see cref="Palette.Gold"/>.</summary>
        public static readonly UnityEngine.Color Gold = Palette.Gold;

        /// <summary>Alias of <see cref="Palette.Purple"/>.</summary>
        public static readonly UnityEngine.Color Purple = Palette.Purple;

        /// <summary>Alias of <see cref="Palette.Red"/>.</summary>
        public static readonly UnityEngine.Color Red = Palette.Red;
    }
}
