namespace HoldMyBeer.Networking
{
    /// <summary>
    /// A friend the local player can invite, as the platform describes them.
    ///
    /// Deliberately platform-agnostic and not replicated: this never crosses the wire,
    /// so it can hold a plain string, unlike the types in §4.3.
    /// </summary>
    public readonly struct PlatformFriend
    {
        public PlatformFriend(ulong id, string name, bool isPlayingThisGame)
        {
            Id = id;
            Name = name;
            IsPlayingThisGame = isPlayingThisGame;
        }

        public ulong Id { get; }

        public string Name { get; }

        /// <summary>Already in the game, so an invite reaches them without a relaunch.</summary>
        public bool IsPlayingThisGame { get; }
    }
}
