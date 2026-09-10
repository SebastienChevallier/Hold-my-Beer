namespace HoldMyBeer.Gameplay
{
    /// <summary>Server-only. Creates the avatar that a client will own.</summary>
    public interface IPlayerSpawner
    {
        void SpawnAllConnectedPlayers();

        void SpawnFor(ulong clientId, int playerIndex);
    }
}
