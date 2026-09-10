using System;
using System.Collections.Generic;
using HoldMyBeer.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace HoldMyBeer.UI
{
    /// <summary>
    /// Player list, join code, and one contextual action: Start for the host,
    /// Ready for everyone else. Rebuilds on <see cref="ILobbyState.Changed"/> only,
    /// never per frame.
    /// </summary>
    public sealed class LobbyScreen : IScreen, IDisposable
    {
        private readonly ILobbyProvider _lobbyProvider;
        private readonly INetworkSessionService _session;
        private readonly Action _onLeave;
        private readonly List<Text> _rows = new();

        private GameObject _root;
        private Transform _listRoot;
        private Text _joinCodeLabel;
        private Text _statusLabel;
        private Button _actionButton;
        private Button _copyCodeButton;
        private Text _actionLabel;
        private ILobbyState _lobby;
        private bool _localReady;

        public LobbyScreen(ILobbyProvider lobbyProvider, INetworkSessionService session, Action onLeave)
        {
            _lobbyProvider = lobbyProvider;
            _session = session;
            _onLeave = onLeave;
        }

        public void Build(Transform root)
        {
            var panel = UiFactory.CreatePanel(root, "Lobby", new Vector2(700f, 720f));
            _root = panel.gameObject;

            UiFactory.CreateLabel(panel, "LOBBY", 44, TextAnchor.MiddleCenter);
            _joinCodeLabel = UiFactory.CreateLabel(panel, string.Empty, 24, TextAnchor.MiddleCenter);
            _copyCodeButton = UiFactory.CreateButton(panel, "COPY CODE", CopyJoinCode);

            UiFactory.CreateLabel(panel, "Players", 22);
            var list = UiFactory.CreatePanel(panel, "PlayerList", new Vector2(640f, 320f));
            var listLayout = list.gameObject.AddComponent<LayoutElement>();
            listLayout.minHeight = 320f;
            _listRoot = list;

            _actionButton = UiFactory.CreateButton(panel, "READY", OnActionClicked);
            _actionLabel = _actionButton.GetComponentInChildren<Text>();

            UiFactory.CreateButton(panel, "LEAVE", () =>
            {
                _session.Leave();
                _onLeave?.Invoke();
            });

            _statusLabel = UiFactory.CreateLabel(panel, string.Empty, 20, TextAnchor.MiddleCenter);

            _lobbyProvider.LobbyChanged += HandleLobbyChanged;
        }

        public void Show()
        {
            _root.SetActive(true);
            _localReady = false;
            HandleLobbyChanged(_lobbyProvider.Current);
        }

        public void Hide()
        {
            if (_lobby != null)
            {
                _lobby.Changed -= Refresh;
                _lobby = null;
            }

            _root.SetActive(false);
        }

        /// <summary>
        /// The provider outlives the scene (it is registered in Boot), so a screen
        /// that forgets to unsubscribe keeps a dead view alive on the next visit.
        /// </summary>
        public void Dispose()
        {
            _lobbyProvider.LobbyChanged -= HandleLobbyChanged;

            if (_lobby != null)
            {
                _lobby.Changed -= Refresh;
                _lobby = null;
            }
        }

        private void HandleLobbyChanged(ILobbyState lobby)
        {
            if (_lobby != null)
            {
                _lobby.Changed -= Refresh;
            }

            _lobby = lobby;

            if (_lobby != null)
            {
                _lobby.Changed += Refresh;
            }

            Refresh();
        }

        private void OnActionClicked()
        {
            if (_lobby == null)
            {
                return;
            }

            if (_session.IsHost)
            {
                _lobby.RequestStartMatch();
                return;
            }

            _localReady = !_localReady;
            _lobby.RequestSetReady(_localReady);
            Refresh();
        }

        /// <summary>
        /// A build has no console to read the code from, and it has to travel to a
        /// friend over chat, so put it on the clipboard.
        /// </summary>
        private void CopyJoinCode()
        {
            if (string.IsNullOrEmpty(_session.JoinCode))
            {
                return;
            }

            GUIUtility.systemCopyBuffer = _session.JoinCode;
            _statusLabel.text = "Join code copied to the clipboard.";
        }

        private void Refresh()
        {
            if (!_root.activeSelf)
            {
                return;
            }

            var hasJoinCode = !string.IsNullOrEmpty(_session.JoinCode);
            _joinCodeLabel.text = hasJoinCode
                ? $"Join code: {_session.JoinCode}"
                : "Direct IP session - share your local or public IP";
            _copyCodeButton.gameObject.SetActive(hasJoinCode);

            if (_lobby == null)
            {
                _statusLabel.text = "Waiting for the host...";
                _actionButton.interactable = false;
                ClearRows();
                return;
            }

            RenderPlayers(_lobby.Players);

            if (_session.IsHost)
            {
                _actionLabel.text = "START";
                _actionButton.interactable = _lobby.CanStartMatch;
                _statusLabel.text = _lobby.CanStartMatch
                    ? "Everyone is ready."
                    : "Waiting for every player to be ready.";
                return;
            }

            _actionLabel.text = _localReady ? "NOT READY" : "READY";
            _actionButton.interactable = true;
            _statusLabel.text = "Waiting for the host to start.";
        }

        private void RenderPlayers(IReadOnlyList<LobbyPlayer> players)
        {
            while (_rows.Count < players.Count)
            {
                _rows.Add(UiFactory.CreateLabel(_listRoot, string.Empty, 24));
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (i >= players.Count)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                var player = players[i];
                var tag = player.IsHost ? "HOST" : player.IsReady ? "READY" : "...";
                row.gameObject.SetActive(true);
                row.text = $"{player.DisplayName}   -   {tag}";
                row.color = player.IsHost || player.IsReady ? UiFactory.Accent : UiFactory.TextColor;
            }
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
            {
                row.gameObject.SetActive(false);
            }
        }
    }
}
