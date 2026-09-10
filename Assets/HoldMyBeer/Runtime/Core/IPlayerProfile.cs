namespace HoldMyBeer.Core
{
    /// <summary>
    /// Local, non-authoritative identity of this machine's player.
    /// The server re-reads the name from the connection payload and owns the truth;
    /// this is only what we send and display before connecting.
    /// </summary>
    public interface IPlayerProfile
    {
        string DisplayName { get; }

        void SetDisplayName(string displayName);
    }
}
