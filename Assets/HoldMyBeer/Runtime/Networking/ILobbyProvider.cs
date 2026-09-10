using System;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// The lobby only exists while a session runs, so the UI cannot resolve it once
    /// at startup. It watches this provider instead and reacts to spawn/despawn.
    /// </summary>
    public interface ILobbyProvider
    {
        ILobbyState Current { get; }

        event Action<ILobbyState> LobbyChanged;
    }

    public sealed class LobbyProvider : ILobbyProvider
    {
        public ILobbyState Current { get; private set; }

        public event Action<ILobbyState> LobbyChanged;

        public void Set(ILobbyState lobby)
        {
            Current = lobby;
            LobbyChanged?.Invoke(lobby);
        }
    }
}
