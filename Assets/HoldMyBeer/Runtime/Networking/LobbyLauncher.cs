using System;
using Unity.Netcode;
using UnityEngine;

namespace HoldMyBeer.Networking
{
    /// <inheritdoc cref="ILobbyLauncher"/>
    public sealed class LobbyLauncher : ILobbyLauncher
    {
        private readonly NetworkManager _networkManager;
        private readonly GameObject _lobbyPrefab;
        private readonly ConnectionApprovalHandler _approval;

        public LobbyLauncher(NetworkManager networkManager, GameObject lobbyPrefab,
                             ConnectionApprovalHandler approval)
        {
            _networkManager = networkManager != null
                ? networkManager
                : throw new ArgumentNullException(nameof(networkManager));
            _lobbyPrefab = lobbyPrefab != null
                ? lobbyPrefab
                : throw new ArgumentNullException(nameof(lobbyPrefab));
            _approval = approval;
        }

        public void SpawnLobby()
        {
            if (!_networkManager.IsServer)
            {
                return;
            }

            var instance = UnityEngine.Object.Instantiate(_lobbyPrefab);

            if (instance.TryGetComponent<LobbyNetworkService>(out var lobby))
            {
                lobby.Configure(_approval);
            }

            if (!instance.TryGetComponent<NetworkObject>(out var networkObject))
            {
                Debug.LogError("The lobby prefab has no NetworkObject.");
                UnityEngine.Object.Destroy(instance);
                return;
            }

            // Dynamically spawned objects are not destroyed on scene load, so the
            // lobby state survives the trip from Menu to Game.
            networkObject.Spawn();
        }
    }
}
