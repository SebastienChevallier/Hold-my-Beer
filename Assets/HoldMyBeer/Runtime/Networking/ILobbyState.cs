using System;
using System.Collections.Generic;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Read model of the lobby plus the two intents a player can express.
    /// The UI depends on this interface only, never on the NetworkBehaviour.
    /// </summary>
    public interface ILobbyState
    {
        /// <summary>False once the match started: used to refuse late joiners.</summary>
        bool IsOpen { get; }

        IReadOnlyList<LobbyPlayer> Players { get; }

        /// <summary>True when the local client is the host and everyone else is ready.</summary>
        bool CanStartMatch { get; }

        event Action Changed;

        /// <summary>Client intent. The server decides.</summary>
        void RequestSetReady(bool isReady);

        /// <summary>Host intent. Ignored by the server if called by anyone else.</summary>
        void RequestStartMatch();
    }
}
