using System;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using Steamworks.Data;

namespace HoldMyBeer.Networking.Steam
{
    /// <summary>
    /// The Steam lobby the session is attached to.
    ///
    /// A lobby is not the connection - the actual traffic goes through
    /// <see cref="SteamNetworkTransport"/>. It is the directory entry that says "this
    /// player is hosting, and here is who to dial", which is what invitations, the
    /// overlay and the friends list all read.
    ///
    /// It is shared because both the transport (which needs it to host or resolve a
    /// host) and the invite service (which needs it to invite and to advertise) act on
    /// the same lobby. Splitting it in two would mean two lobbies for one session.
    /// </summary>
    internal sealed class SteamLobbyService
    {
        private Lobby? _lobby;

        /// <summary>0 when not in a lobby.</summary>
        internal ulong CurrentLobbyId => _lobby?.Id.Value ?? 0UL;

        /// <summary>The host's SteamID, which is who a client dials. 0 when not in a lobby.</summary>
        internal SteamId HostId => _lobby?.Owner.Id ?? default;

        internal async Task<bool> CreateAsync(int maxPlayers)
        {
            Leave();

            try
            {
                var created = await SteamMatchmaking.CreateLobbyAsync(Mathf.Max(2, maxPlayers));
                if (!created.HasValue)
                {
                    Debug.LogError("[Steam] Steam refused to create the lobby.");
                    return false;
                }

                var lobby = created.Value;

                // Friends-only rather than public: with the Spacewar AppID the lobby
                // list is shared with every other test project on Steam, and we do not
                // want strangers walking into a game between friends.
                lobby.SetFriendsOnly();
                lobby.SetJoinable(true);

                _lobby = lobby;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Steam] Lobby creation failed: {exception.Message}");
                return false;
            }
        }

        /// <summary>Enters a lobby and returns its host, or default if it could not be joined.</summary>
        internal async Task<SteamId> JoinAsync(ulong lobbyId)
        {
            Leave();

            try
            {
                var joined = await SteamMatchmaking.JoinLobbyAsync(lobbyId);
                if (!joined.HasValue)
                {
                    Debug.LogError($"[Steam] Could not enter lobby {lobbyId}.");
                    return default;
                }

                _lobby = joined.Value;

                var host = joined.Value.Owner.Id;
                if (host.Value == 0UL)
                {
                    Debug.LogError($"[Steam] Lobby {lobbyId} reports no owner to connect to.");
                    Leave();
                    return default;
                }

                return host;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Steam] Joining lobby {lobbyId} failed: {exception.Message}");
                return default;
            }
        }

        /// <summary>
        /// Sends a Steam invitation to the current lobby. Steam decides how it lands:
        /// a friend already in the game gets OnGameLobbyJoinRequested, one who is not
        /// gets the game launched with "+connect_lobby".
        /// </summary>
        internal bool InviteFriend(ulong friendId)
        {
            if (!_lobby.HasValue)
            {
                Debug.LogWarning("[Steam] No lobby to invite anyone into yet.");
                return false;
            }

            try
            {
                return _lobby.Value.InviteFriend(friendId);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Steam] Inviting {friendId} failed: {exception.Message}");
                return false;
            }
        }

        internal void Leave()
        {
            if (!_lobby.HasValue)
            {
                return;
            }

            try
            {
                _lobby.Value.Leave();
            }
            catch (Exception exception)
            {
                // Leaving a lobby that Steam already tore down is not worth a failure.
                Debug.LogWarning($"[Steam] Leaving the lobby failed: {exception.Message}");
            }

            _lobby = null;
        }
    }
}
