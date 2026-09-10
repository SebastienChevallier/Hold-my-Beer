using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Steamworks;
using Steamworks.Data;

namespace HoldMyBeer.Networking.Steam
{
    /// <summary>
    /// The "play with a friend" side of Steam: the invite overlay, the Join Game
    /// button in the friends list, and accepted invitations coming back in.
    ///
    /// Steam offers two ways in, and they are not interchangeable. If the game is
    /// already running, Steam raises OnGameLobbyJoinRequested. If it is not, Steam
    /// *launches* the game with "+connect_lobby &lt;id&gt;" on the command line - long
    /// before any menu exists to react - so that one is parked until the menu claims it.
    /// </summary>
    public sealed class SteamInviteService : ISessionInviteService, IPlatformIdentity, IDisposable
    {
        private const string ConnectArgument = "+connect_lobby";
        private const string RichPresenceConnectKey = "connect";

        private readonly NetworkManager _networkManager;

        private ulong _pendingLobbyId;

        public SteamInviteService(NetworkManager networkManager)
        {
            _networkManager = networkManager != null
                ? networkManager
                : throw new ArgumentNullException(nameof(networkManager));

            _pendingLobbyId = ReadLaunchInvite();

            SteamFriends.OnGameLobbyJoinRequested += HandleJoinRequested;

            // The lobby must not outlive the session it advertises, otherwise friends
            // keep seeing a Join button that leads nowhere.
            _networkManager.OnServerStopped += HandleSessionStopped;
            _networkManager.OnClientStopped += HandleSessionStopped;
        }

        public bool IsAvailable => SteamRuntime.IsAvailable;

        public bool HasIdentity => SteamRuntime.IsAvailable;

        public string DisplayName => SteamRuntime.IsAvailable ? SteamClient.Name : string.Empty;

        public event Action<ulong> JoinRequested;

        /// <summary>
        /// Friends who can actually receive an invitation right now: online, and with
        /// the ones already in the game first, since for them an invite arrives
        /// instantly instead of trying to launch anything.
        ///
        /// Offline friends are left out rather than shown greyed: an invitation to
        /// someone offline goes nowhere, and a list of hundreds is not a picker.
        /// </summary>
        public IReadOnlyList<PlatformFriend> ListInvitableFriends()
        {
            if (!SteamRuntime.IsAvailable)
            {
                return Array.Empty<PlatformFriend>();
            }

            var friends = new List<PlatformFriend>();

            foreach (var friend in SteamFriends.GetFriends())
            {
                if (friend.IsOnline)
                {
                    friends.Add(new PlatformFriend(friend.Id.Value, friend.Name, friend.IsPlayingThisGame));
                }
            }

            friends.Sort(CompareFriends);
            return friends;
        }

        public bool Invite(ulong friendId)
        {
            var sent = SteamRuntime.Lobby.InviteFriend(friendId);
            Debug.Log($"[Steam] Invitation to {friendId} {(sent ? "sent" : "refused by Steam")}.");
            return sent;
        }

        private static int CompareFriends(PlatformFriend left, PlatformFriend right)
        {
            if (left.IsPlayingThisGame != right.IsPlayingThisGame)
            {
                return left.IsPlayingThisGame ? -1 : 1;
            }

            return string.Compare(left.Name, right.Name, StringComparison.CurrentCultureIgnoreCase);
        }

        public void PublishJoinableSession()
        {
            var lobbyId = SteamRuntime.Lobby.CurrentLobbyId;
            if (lobbyId == 0UL)
            {
                return;
            }

            // This exact key is what makes Steam show "Join Game" next to our name,
            // and what it puts on the command line when a friend clicks it.
            SteamFriends.SetRichPresence(RichPresenceConnectKey, $"{ConnectArgument} {lobbyId}");
        }

        public void ClearJoinableSession() => SteamFriends.ClearRichPresence();

        public bool TryConsumePendingInvite(out ulong lobbyId)
        {
            lobbyId = _pendingLobbyId;
            _pendingLobbyId = 0UL;
            return lobbyId != 0UL;
        }

        public void Dispose()
        {
            SteamFriends.OnGameLobbyJoinRequested -= HandleJoinRequested;

            if (_networkManager != null)
            {
                _networkManager.OnServerStopped -= HandleSessionStopped;
                _networkManager.OnClientStopped -= HandleSessionStopped;
            }
        }

        private void HandleJoinRequested(Lobby lobby, SteamId _)
        {
            var lobbyId = lobby.Id.Value;

            if (JoinRequested == null)
            {
                // The menu is not listening yet (still booting): park it rather than
                // dropping the invitation on the floor.
                _pendingLobbyId = lobbyId;
                return;
            }

            JoinRequested.Invoke(lobbyId);
        }

        private void HandleSessionStopped(bool _)
        {
            ClearJoinableSession();
            SteamRuntime.Lobby.Leave();
        }

        /// <summary>
        /// Reads the invitation Steam used to start the game, if there was one.
        ///
        /// Two sources, because Steam uses both: the process command line when it
        /// launches the executable, and <c>SteamApps.CommandLine</c> when it hands the
        /// arguments over through the API instead - which is what Valve documents as
        /// the one to read, and what suppresses the client's launch-argument warning.
        /// </summary>
        private static ulong ReadLaunchInvite()
        {
            if (TryReadLobby(Environment.GetCommandLineArgs(), out var lobbyId) ||
                TryReadLobby(SafeSteamCommandLine(), out lobbyId))
            {
                Debug.Log($"[Steam] Launched from an invitation to lobby {lobbyId}.");
                return lobbyId;
            }

            return 0UL;
        }

        private static string[] SafeSteamCommandLine()
        {
            if (!SteamRuntime.IsAvailable)
            {
                return Array.Empty<string>();
            }

            var commandLine = SteamApps.CommandLine;
            return string.IsNullOrEmpty(commandLine)
                ? Array.Empty<string>()
                : commandLine.Split(' ');
        }

        private static bool TryReadLobby(string[] arguments, out ulong lobbyId)
        {
            for (var i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == ConnectArgument && ulong.TryParse(arguments[i + 1], out lobbyId))
                {
                    return true;
                }
            }

            lobbyId = 0UL;
            return false;
        }
    }
}
