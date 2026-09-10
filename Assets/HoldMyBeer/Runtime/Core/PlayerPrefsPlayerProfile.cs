using UnityEngine;

namespace HoldMyBeer.Core
{
    /// <inheritdoc cref="IPlayerProfile"/>
    public sealed class PlayerPrefsPlayerProfile : IPlayerProfile
    {
        public const int MaxNameLength = 20;

        private const string PrefsKey = "HoldMyBeer.DisplayName";

        public PlayerPrefsPlayerProfile()
        {
            DisplayName = Sanitize(PlayerPrefs.GetString(PrefsKey, string.Empty));
        }

        public string DisplayName { get; private set; }

        public void SetDisplayName(string displayName)
        {
            DisplayName = Sanitize(displayName);
            PlayerPrefs.SetString(PrefsKey, DisplayName);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Never trust a name coming from the network either: the server calls this
        /// on the payload it receives before publishing it in the lobby.
        /// </summary>
        public static string Sanitize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return $"Player{Random.Range(1000, 9999)}";
            }

            var trimmed = raw.Trim();
            return trimmed.Length > MaxNameLength ? trimmed[..MaxNameLength] : trimmed;
        }
    }
}
