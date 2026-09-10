using System;
using System.Threading.Tasks;
using Unity.Netcode;

namespace HoldMyBeer.Networking.Steam
{
    /// <summary>
    /// Play over Steam: the host opens a lobby and a relay socket, friends dial the
    /// host's SteamID through Valve's relay. No port to open, no account to link
    /// beyond Steam itself.
    ///
    /// Like Relay, this lives in its own assembly: delete the Steam folder and the
    /// game still compiles and runs on the other modes.
    /// </summary>
    public sealed class SteamSessionTransport : ISessionTransport
    {
        private readonly NetworkManager _networkManager;

        public SteamSessionTransport(NetworkManager networkManager)
        {
            _networkManager = networkManager != null
                ? networkManager
                : throw new ArgumentNullException(nameof(networkManager));
        }

        public SessionMode Mode => SessionMode.Steam;

        public async Task<SessionResult> ConfigureHostAsync(SessionRequest request)
        {
            if (!SteamRuntime.IsAvailable)
            {
                return SessionResult.Fail("Steam is not running.");
            }

            if (!await SteamRuntime.Lobby.CreateAsync(request.MaxPlayers))
            {
                return SessionResult.Fail("Steam refused to open a lobby. Is Steam still running?");
            }

            NetworkTransportActivator.Activate<SteamNetworkTransport>(_networkManager);

            // The lobby id doubles as the join code: a friend who cannot use the
            // overlay can still paste it, and both paths end up in the same lobby.
            return SessionResult.Ok(SteamRuntime.Lobby.CurrentLobbyId.ToString());
        }

        public async Task<SessionResult> ConfigureClientAsync(SessionRequest request)
        {
            if (!SteamRuntime.IsAvailable)
            {
                return SessionResult.Fail("Steam is not running.");
            }

            if (request.LobbyId == 0UL)
            {
                return SessionResult.Fail("Enter the lobby code given by the host, or accept their invite.");
            }

            var host = await SteamRuntime.Lobby.JoinAsync(request.LobbyId);
            if (host.Value == 0UL)
            {
                return SessionResult.Fail($"Could not join lobby '{request.LobbyId}'. It may be closed or full.");
            }

            // Activate before addressing: the previous attempt may have left another
            // mode's transport in place, and TargetSteamId belongs to this one.
            NetworkTransportActivator.Activate<SteamNetworkTransport>(_networkManager).TargetSteamId = host;

            return SessionResult.Ok(request.LobbyId.ToString());
        }
    }
}
