namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Host-side creation of the replicated lobby object. Split from
    /// <see cref="INetworkSessionService"/> so the session service stays about
    /// connectivity and knows nothing about lobby content.
    /// </summary>
    public interface ILobbyLauncher
    {
        void SpawnLobby();
    }
}
