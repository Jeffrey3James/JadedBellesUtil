using System;
using System.Text;
using UnityEngine;

namespace JadedBelles.Util.Auth
{
    /// <summary>
    /// Persists an access/refresh token pair across launches via <see cref="PlayerPrefs"/>.
    /// Base64 is applied for light obfuscation only — it is not a security boundary against
    /// anyone who can read PlayerPrefs on disk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The store is instance-based so that multiple JadedBelles titles can share a device
    /// without their session prefs colliding. Pass a title-unique <c>keyPrefix</c> to the
    /// constructor (for example <c>"match3."</c> or <c>"arena."</c>) and the prefix is
    /// prepended to both the access-token and refresh-token PlayerPrefs keys.
    /// </para>
    /// <para>
    /// The default prefix is <see cref="DefaultKeyPrefix"/> (<c>"jadedbelles."</c>), which is
    /// suitable for a brand-new consumer that has no existing installs to migrate.
    /// </para>
    /// <para>
    /// <b>Migration path from the pre-v0.4.0 static <c>TokenStore</c>:</b> the legacy
    /// implementation used the hardcoded PlayerPrefs keys <c>jb_access_token</c> and
    /// <c>jb_refresh_token</c>. Existing callers that need to keep reading their old data
    /// verbatim should construct the store with an empty prefix — <c>new TokenStore(string.Empty)</c>
    /// — which yields exactly those two keys. New titles should adopt a distinct prefix
    /// so previously installed JadedBelles games on the same device do not share a session.
    /// </para>
    /// </remarks>
    public sealed class TokenStore
    {
        /// <summary>Default prefix used when the parameterless constructor is invoked.</summary>
        public const string DefaultKeyPrefix = "jadedbelles.";

        private const string AccessTokenSuffix = "jb_access_token";
        private const string RefreshTokenSuffix = "jb_refresh_token";

        /// <summary>The prefix prepended to every PlayerPrefs key this instance writes.</summary>
        public string KeyPrefix { get; }

        /// <summary>The fully-qualified PlayerPrefs key used for the access token.</summary>
        public string AccessTokenKey { get; }

        /// <summary>The fully-qualified PlayerPrefs key used for the refresh token.</summary>
        public string RefreshTokenKey { get; }

        /// <summary>
        /// Creates a store that uses <see cref="DefaultKeyPrefix"/> in front of its PlayerPrefs
        /// keys. Suitable for a brand-new consumer with no existing installs to migrate.
        /// </summary>
        public TokenStore() : this(DefaultKeyPrefix) { }

        /// <summary>
        /// Creates a store that prepends <paramref name="keyPrefix"/> to its PlayerPrefs keys.
        /// Pass a title-unique prefix (e.g. <c>"match3."</c>) to isolate sessions on devices
        /// that have more than one JadedBelles title installed. Pass <see cref="string.Empty"/>
        /// to preserve the legacy pre-v0.4.0 key names (<c>jb_access_token</c> / <c>jb_refresh_token</c>).
        /// </summary>
        /// <param name="keyPrefix">The prefix to prepend to both PlayerPrefs keys. Must not be null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="keyPrefix"/> is null.</exception>
        public TokenStore(string keyPrefix)
        {
            if (keyPrefix == null)
                throw new ArgumentNullException(nameof(keyPrefix));

            KeyPrefix = keyPrefix;
            AccessTokenKey = keyPrefix + AccessTokenSuffix;
            RefreshTokenKey = keyPrefix + RefreshTokenSuffix;
        }

        /// <summary>
        /// Writes the given access/refresh token pair to PlayerPrefs (Base64-encoded).
        /// If either token is null or empty, both keys are cleared instead.
        /// </summary>
        public void SaveTokens(string access, string refresh)
        {
            if (string.IsNullOrEmpty(access) || string.IsNullOrEmpty(refresh))
            {
                Clear();
                return;
            }

            PlayerPrefs.SetString(AccessTokenKey, Encode(access));
            PlayerPrefs.SetString(RefreshTokenKey, Encode(refresh));
            PlayerPrefs.Save();
        }

        /// <summary>Returns the decoded access token, or the empty string if none is stored or the value is corrupt.</summary>
        public string GetAccessToken()
        {
            return Decode(PlayerPrefs.GetString(AccessTokenKey, string.Empty));
        }

        /// <summary>Returns the decoded refresh token, or the empty string if none is stored or the value is corrupt.</summary>
        public string GetRefreshToken()
        {
            return Decode(PlayerPrefs.GetString(RefreshTokenKey, string.Empty));
        }

        /// <summary>Removes both tokens from PlayerPrefs.</summary>
        public void Clear()
        {
            PlayerPrefs.DeleteKey(AccessTokenKey);
            PlayerPrefs.DeleteKey(RefreshTokenKey);
            PlayerPrefs.Save();
        }

        /// <summary>True when both an access token and a refresh token are stored and decode to non-empty strings.</summary>
        public bool HasSession()
        {
            return !string.IsNullOrEmpty(GetAccessToken()) && !string.IsNullOrEmpty(GetRefreshToken());
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static string Decode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch (FormatException)
            {
                // Old/corrupt preferences should be treated as an expired session.
                return string.Empty;
            }
        }
    }
}
