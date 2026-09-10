using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HoldMyBeer.Core;
using HoldMyBeer.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace HoldMyBeer.UI
{
    /// <summary>
    /// Two actions: create a session, or join one. The extra fields are the minimum
    /// the transports actually need (a name, and an address, a join code or a lobby).
    ///
    /// The mode list is not hardcoded: it cycles over the transports that actually
    /// registered, so a mode whose installer bowed out (Steam not running, Relay
    /// folder deleted) is never offered to the player.
    /// </summary>
    public sealed class MainMenuScreen : IScreen, IDisposable
    {
        private const int MaxPlayers = 8;

        private readonly INetworkSessionService _session;
        private readonly ISessionTransportProvider _transports;
        private readonly ISessionInviteService _invites;
        private readonly ILobbyLauncher _lobbyLauncher;
        private readonly IPlayerProfile _profile;
        private readonly Action _onSessionOpened;

        private readonly List<SessionMode> _offeredModes = new();

        private GameObject _root;
        private Text _nameLabel;
        private InputField _nameField;
        private InputField _addressField;
        private Text _statusLabel;
        private Text _modeLabel;
        private Button _hostButton;
        private Button _joinButton;
        private Button _switchModeButton;
        private SessionMode _mode = SessionMode.DirectIp;
        private bool _busy;

        public MainMenuScreen(INetworkSessionService session, ISessionTransportProvider transports,
                              ISessionInviteService invites, ILobbyLauncher lobbyLauncher,
                              IPlayerProfile profile, Action onSessionOpened)
        {
            _session = session;
            _transports = transports;
            _invites = invites;
            _lobbyLauncher = lobbyLauncher;
            _profile = profile;
            _onSessionOpened = onSessionOpened;
        }

        public void Build(Transform root)
        {
            var panel = UiFactory.CreatePanel(root, "MainMenu", new Vector2(620f, 620f));
            _root = panel.gameObject;

            UiFactory.CreateLabel(panel, "HOLD MY BEER", 52, TextAnchor.MiddleCenter);
            _nameLabel = UiFactory.CreateLabel(panel, "Name", 20);
            _nameField = UiFactory.CreateInputField(panel, "Your name", _profile.DisplayName);

            // With a platform account there is nothing to ask: the name is already the
            // one the player's friends know, and letting them retype it only invites
            // mismatches.
            var usesPlatformName = _invites is IPlatformIdentity { HasIdentity: true };
            _nameLabel.gameObject.SetActive(!usesPlatformName);
            _nameField.gameObject.SetActive(!usesPlatformName);

            _modeLabel = UiFactory.CreateLabel(panel, string.Empty, 20);
            _switchModeButton = UiFactory.CreateButton(panel, "Switch mode", CycleMode);

            _addressField = UiFactory.CreateInputField(panel, "Host IP", SessionRequest.LoopbackAddress);

            _hostButton = UiFactory.CreateButton(panel, "CREATE", () => Run(HostAsync));
            _joinButton = UiFactory.CreateButton(panel, "JOIN", () => Run(JoinAsync));

            _statusLabel = UiFactory.CreateLabel(panel, string.Empty, 20, TextAnchor.MiddleCenter);

            RefreshOfferedModes();
            _mode = DefaultMode();

            // Only worth offering when there is somewhere to switch to.
            _switchModeButton.gameObject.SetActive(_offeredModes.Count > 1);

            _invites.JoinRequested += HandleInviteAccepted;

            RefreshMode();
        }

        public void Show()
        {
            _root.SetActive(true);
            _busy = false;
            SetInteractable(true);

            // An invitation may have launched the game outright, in which case it has
            // been waiting since boot for this screen to exist.
            if (_invites.TryConsumePendingInvite(out var lobbyId))
            {
                HandleInviteAccepted(lobbyId);
            }
        }

        public void Hide() => _root.SetActive(false);

        /// <summary>
        /// The invite service is registered in Boot and outlives the Menu scene, so a
        /// screen that forgets to unsubscribe would be revived by the next invitation.
        /// </summary>
        public void Dispose() => _invites.JoinRequested -= HandleInviteAccepted;

        public void SetStatus(string message)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = message;
            }
        }

        /// <summary>
        /// Steam is the mode the game ships with; Direct IP and Relay exist so we can
        /// iterate without a Steam client, and a player should never be handed those
        /// as a choice. They stay reachable in the editor and in development builds,
        /// which is exactly where they are useful.
        ///
        /// If Steam is not there at all, everything else is offered anyway: shipping a
        /// menu with no way to play would be a worse failure than showing a dev mode.
        /// </summary>
        private void RefreshOfferedModes()
        {
            _offeredModes.Clear();

            var available = _transports.AvailableModes;

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            for (var i = 0; i < available.Count; i++)
            {
                if (available[i] == SessionMode.Steam)
                {
                    _offeredModes.Add(SessionMode.Steam);
                    return;
                }
            }
#endif

            for (var i = 0; i < available.Count; i++)
            {
                _offeredModes.Add(available[i]);
            }
        }

        /// <summary>
        /// Steam whenever it is there, even in the editor: the dev modes stay one click
        /// away, but nobody should have to cycle to reach the mode the game ships with.
        /// </summary>
        private SessionMode DefaultMode()
        {
            for (var i = 0; i < _offeredModes.Count; i++)
            {
                if (_offeredModes[i] == SessionMode.Steam)
                {
                    return SessionMode.Steam;
                }
            }

            return _offeredModes.Count > 0 ? _offeredModes[0] : SessionMode.DirectIp;
        }

        private void CycleMode()
        {
            var modes = _offeredModes;
            if (modes.Count == 0)
            {
                return;
            }

            var index = 0;
            for (var i = 0; i < modes.Count; i++)
            {
                if (modes[i] == _mode)
                {
                    index = i;
                    break;
                }
            }

            _mode = modes[(index + 1) % modes.Count];
            RefreshMode();
        }

        private void RefreshMode()
        {
            _modeLabel.text = _mode switch
            {
                SessionMode.DirectIp => "Mode: Direct IP (LAN, no account needed)",
                SessionMode.Relay => "Mode: Relay (over the internet, needs a linked UGS project)",
                SessionMode.Steam => "Mode: Steam (invite friends from the overlay)",
                _ => $"Mode: {_mode}"
            };

            _addressField.placeholder.GetComponent<Text>().text = _mode switch
            {
                SessionMode.DirectIp => "Host IP",
                SessionMode.Relay => "Join code",
                SessionMode.Steam => "Lobby code (or accept an invite)",
                _ => string.Empty
            };

            // Switching modes must not leave the previous mode's value behind: a
            // leftover "127.0.0.1" would be sent to Relay as a join code.
            if (_mode == SessionMode.DirectIp)
            {
                if (string.IsNullOrWhiteSpace(_addressField.text))
                {
                    _addressField.text = SessionRequest.LoopbackAddress;
                }
            }
            else if (_addressField.text == SessionRequest.LoopbackAddress)
            {
                _addressField.text = string.Empty;
            }
        }

        /// <summary>
        /// A friend clicked Join in Steam. The project has neither host migration nor
        /// reconnection, so hopping straight from a live session into another one is
        /// not something we can do cleanly yet: say so instead of half-doing it.
        /// </summary>
        private void HandleInviteAccepted(ulong lobbyId)
        {
            if (_busy || _session.Status != SessionStatus.Offline)
            {
                SetStatus("Invitation ignored: leave the current session first.");
                return;
            }

            _mode = SessionMode.Steam;
            RefreshMode();
            _addressField.text = lobbyId.ToString();

            Run(() => JoinLobbyAsync(lobbyId));
        }

        private async void Run(Func<Task> action)
        {
            if (_busy)
            {
                return;
            }

            _busy = true;
            SetInteractable(false);
            SetStatus("Connecting...");

            try
            {
                await action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetStatus($"Unexpected error: {exception.Message}");
            }
            finally
            {
                _busy = false;
                SetInteractable(true);
            }
        }

        private async Task HostAsync()
        {
            ApplyName();

            var request = _mode switch
            {
                SessionMode.Relay => SessionRequest.HostRelay(MaxPlayers),
                SessionMode.Steam => SessionRequest.HostSteam(MaxPlayers),
                _ => SessionRequest.HostDirectIp(MaxPlayers)
            };

            var result = await _session.HostAsync(request);
            if (!result.Success)
            {
                SetStatus(result.Error);
                return;
            }

            // Tell the platform we are joinable, which is what puts a Join Game button
            // next to our name in the friends list.
            if (_mode == SessionMode.Steam)
            {
                _invites.PublishJoinableSession();
            }

            _lobbyLauncher.SpawnLobby();
            _onSessionOpened?.Invoke();
        }

        private async Task JoinAsync()
        {
            var value = _addressField.text;

            if (_mode == SessionMode.Steam)
            {
                if (!ulong.TryParse((value ?? string.Empty).Trim(), out var lobbyId))
                {
                    SetStatus("That is not a lobby code. Paste the host's code, or accept their invite.");
                    return;
                }

                await JoinLobbyAsync(lobbyId);
                return;
            }

            ApplyName();

            var request = _mode == SessionMode.Relay
                ? SessionRequest.JoinRelay(value)
                : SessionRequest.JoinDirectIp(value);

            await CompleteJoinAsync(request);
        }

        private async Task JoinLobbyAsync(ulong lobbyId)
        {
            ApplyName();
            await CompleteJoinAsync(SessionRequest.JoinSteam(lobbyId));
        }

        private async Task CompleteJoinAsync(SessionRequest request)
        {
            var result = await _session.JoinAsync(request);
            if (!result.Success)
            {
                SetStatus(result.Error);
                return;
            }

            _onSessionOpened?.Invoke();
        }

        /// <summary>
        /// Only for modes without a platform account. With Steam the profile was already
        /// set from the account at boot, and overwriting it from a hidden field would
        /// silently blank the name.
        /// </summary>
        private void ApplyName()
        {
            if (_nameField.gameObject.activeSelf)
            {
                _profile.SetDisplayName(_nameField.text);
            }
        }

        private void SetInteractable(bool interactable)
        {
            _hostButton.interactable = interactable;
            _joinButton.interactable = interactable;
        }
    }
}
