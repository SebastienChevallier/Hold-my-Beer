using System;
using System.Threading.Tasks;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Owns the lifetime of a multiplayer session. The UI talks to this and never
    /// to <c>NetworkManager</c> directly, so no screen has to know whether we run
    /// on Direct IP, Relay, or anything else.
    /// </summary>
    public interface INetworkSessionService
    {
        SessionStatus Status { get; }

        bool IsHost { get; }

        /// <summary>Relay join code of the current session. Empty in Direct IP mode.</summary>
        string JoinCode { get; }

        /// <summary>Raised on every client when the local session ends, with the reason if any.</summary>
        event Action<string> SessionEnded;

        event Action<SessionStatus> StatusChanged;

        Task<SessionResult> HostAsync(SessionRequest request);

        Task<SessionResult> JoinAsync(SessionRequest request);

        void Leave();

        /// <summary>
        /// Server-only. Loads a scene for every connected client through the
        /// network scene manager, keeping them in sync.
        /// </summary>
        void LoadNetworkScene(string sceneName);
    }
}
