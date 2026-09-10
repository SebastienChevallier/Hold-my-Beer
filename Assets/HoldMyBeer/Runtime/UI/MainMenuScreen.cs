using System;
using HoldMyBeer.Core;
using HoldMyBeer.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace HoldMyBeer.UI
{
    /// <summary>
    /// Two actions: create a session, or join one. The extra fields are the minimum
    /// the transports actually need (a name, and an address or a join code).
    /// </summary>
    public sealed class MainMenuScreen : IScreen
    {
        private const int MaxPlayers = 8;

        private readonly INetworkSessionService _session;
        private readonly ILobbyLauncher _lobbyLauncher;
        private readonly IPlayerProfile _profile;
        private readonly Action _onSessionOpened;

        private GameObject _root;
        private InputField _nameField;
        private InputField _addressField;
        private Text _statusLabel;
        private Text _modeLabel;
        private Button _hostButton;
        private Button _joinButton;
        private SessionMode _mode = SessionMode.DirectIp;
        private bool _busy;

        public MainMenuScreen(INetworkSessionService session, ILobbyLauncher lobbyLauncher,
                              IPlayerProfile profile, Action onSessionOpened)
        {
            _session = session;
            _lobbyLauncher = lobbyLauncher;
            _profile = profile;
            _onSessionOpened = onSessionOpened;
        }

        public void Build(Transform root)
        {
            var panel = UiFactory.CreatePanel(root, "MainMenu", new Vector2(620f, 620f));
            _root = panel.gameObject;

            UiFactory.CreateLabel(panel, "HOLD MY BEER", 52, TextAnchor.MiddleCenter);
            UiFactory.CreateLabel(panel, "Name", 20);
            _nameField = UiFactory.CreateInputField(panel, "Your name", _profile.DisplayName);

            _modeLabel = UiFactory.CreateLabel(panel, string.Empty, 20);
            UiFactory.CreateButton(panel, "Switch mode", ToggleMode);

            _addressField = UiFactory.CreateInputField(panel, "Host IP", SessionRequest.LoopbackAddress);

            _hostButton = UiFactory.CreateButton(panel, "CREATE", () => Run(HostAsync));
            _joinButton = UiFactory.CreateButton(panel, "JOIN", () => Run(JoinAsync));

            _statusLabel = UiFactory.CreateLabel(panel, string.Empty, 20, TextAnchor.MiddleCenter);

            RefreshMode();
        }

        public void Show()
        {
            _root.SetActive(true);
            _busy = false;
            SetInteractable(true);
        }

        public void Hide() => _root.SetActive(false);

        public void SetStatus(string message)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = message;
            }
        }

        private void ToggleMode()
        {
            _mode = _mode == SessionMode.DirectIp ? SessionMode.Relay : SessionMode.DirectIp;
            RefreshMode();
        }

        private void RefreshMode()
        {
            var isDirect = _mode == SessionMode.DirectIp;
            _modeLabel.text = isDirect
                ? "Mode: Direct IP (LAN, no account needed)"
                : "Mode: Relay (over the internet, needs a linked UGS project)";
            _addressField.placeholder.GetComponent<Text>().text = isDirect ? "Host IP" : "Join code";

            // Switching modes must not leave the previous mode's value behind: a
            // leftover "127.0.0.1" would be sent to Relay as a join code.
            if (isDirect)
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

        private async void Run(Func<System.Threading.Tasks.Task> action)
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

        private async System.Threading.Tasks.Task HostAsync()
        {
            ApplyName();

            var request = _mode == SessionMode.DirectIp
                ? SessionRequest.HostDirectIp(MaxPlayers)
                : SessionRequest.HostRelay(MaxPlayers);

            var result = await _session.HostAsync(request);
            if (!result.Success)
            {
                SetStatus(result.Error);
                return;
            }

            _lobbyLauncher.SpawnLobby();
            _onSessionOpened?.Invoke();
        }

        private async System.Threading.Tasks.Task JoinAsync()
        {
            ApplyName();

            var value = _addressField.text;
            var request = _mode == SessionMode.DirectIp
                ? SessionRequest.JoinDirectIp(value)
                : SessionRequest.JoinRelay(value);

            var result = await _session.JoinAsync(request);
            if (!result.Success)
            {
                SetStatus(result.Error);
                return;
            }

            _onSessionOpened?.Invoke();
        }

        private void ApplyName() => _profile.SetDisplayName(_nameField.text);

        private void SetInteractable(bool interactable)
        {
            _hostButton.interactable = interactable;
            _joinButton.interactable = interactable;
        }
    }
}
