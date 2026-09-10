using System;
using System.Collections.Generic;
using HoldMyBeer.Core;
using Unity.Netcode;
using UnityEngine;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Server-authoritative lobby. Spawned by the host right after the session opens
    /// and, being dynamically spawned, survives the scene change into the game.
    /// Clients never write to the list: they send intents, the server applies them.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class LobbyNetworkService : NetworkBehaviour, ILobbyState
    {
        private readonly NetworkVariable<bool> _isOpen = new(true);
        private readonly NetworkList<LobbyPlayer> _players = new();
        private readonly List<LobbyPlayer> _snapshot = new();

        private ConnectionApprovalHandler _approval;
        private LobbyProvider _provider;

        public bool IsOpen => _isOpen.Value;

        public IReadOnlyList<LobbyPlayer> Players => _snapshot;

        public bool CanStartMatch
        {
            get
            {
                if (!IsHost || _snapshot.Count == 0)
                {
                    return false;
                }

                foreach (var player in _snapshot)
                {
                    // The host is implicitly ready: pressing Start is the readiness.
                    if (!player.IsHost && !player.IsReady)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public event Action Changed;

        /// <summary>Server-only wiring, injected by the session installer before spawn.</summary>
        public void Configure(ConnectionApprovalHandler approval) => _approval = approval;

        public override void OnNetworkSpawn()
        {
            _players.OnListChanged += HandleListChanged;
            _isOpen.OnValueChanged += HandleOpenChanged;

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += AddPlayer;
                NetworkManager.OnClientDisconnectCallback += RemovePlayer;
                AddPlayer(NetworkManager.LocalClientId);
            }

            RebuildSnapshot();

            if (AppServices.IsReady && AppServices.Container.TryResolve<ILobbyProvider>(out var provider))
            {
                _provider = provider as LobbyProvider;
                _provider?.Set(this);
            }
        }

        public override void OnNetworkDespawn()
        {
            _players.OnListChanged -= HandleListChanged;
            _isOpen.OnValueChanged -= HandleOpenChanged;

            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= AddPlayer;
                NetworkManager.OnClientDisconnectCallback -= RemovePlayer;
            }

            _provider?.Set(null);
            _provider = null;
        }

        public void RequestSetReady(bool isReady) => SetReadyRpc(isReady);

        public void RequestStartMatch() => StartMatchRpc();

        [Rpc(SendTo.Server)]
        private void SetReadyRpc(bool isReady, RpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;
            for (var i = 0; i < _players.Count; i++)
            {
                if (_players[i].ClientId != senderId)
                {
                    continue;
                }

                var player = _players[i];
                player.IsReady = isReady;
                _players[i] = player;
                return;
            }
        }

        [Rpc(SendTo.Server)]
        private void StartMatchRpc(RpcParams rpcParams = default)
        {
            // Authority check: only the host may start, whatever the client claims.
            if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
            {
                Debug.LogWarning($"Client {rpcParams.Receive.SenderClientId} tried to start the match. Ignored.");
                return;
            }

            if (!CanStartMatch)
            {
                return;
            }

            _isOpen.Value = false;

            if (AppServices.IsReady && AppServices.Container.TryResolve<INetworkSessionService>(out var session))
            {
                session.LoadNetworkScene(SceneNames.Game);
            }
        }

        private void AddPlayer(ulong clientId)
        {
            foreach (var existing in _players)
            {
                if (existing.ClientId == clientId)
                {
                    return;
                }
            }

            var displayName = _approval != null ? _approval.GetDisplayName(clientId) : $"Player {clientId}";
            _players.Add(new LobbyPlayer(clientId, displayName, clientId == NetworkManager.ServerClientId));
        }

        private void RemovePlayer(ulong clientId)
        {
            for (var i = _players.Count - 1; i >= 0; i--)
            {
                if (_players[i].ClientId == clientId)
                {
                    _players.RemoveAt(i);
                }
            }
        }

        private void HandleListChanged(NetworkListEvent<LobbyPlayer> changeEvent) => RebuildSnapshot();

        private void HandleOpenChanged(bool previous, bool current) => Changed?.Invoke();

        private void RebuildSnapshot()
        {
            _snapshot.Clear();
            foreach (var player in _players)
            {
                _snapshot.Add(player);
            }

            Changed?.Invoke();
        }
    }
}
