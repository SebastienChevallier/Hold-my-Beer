using System;
using HoldMyBeer.Core;
using Unity.Netcode;
using UnityEngine;

namespace HoldMyBeer.Gameplay
{
    /// <summary>
    /// Instantiates and spawns player avatars. Server-only by construction: calling
    /// it from a client is a bug, not a supported path, so it complains loudly.
    /// </summary>
    public sealed class PlayerSpawner : IPlayerSpawner
    {
        private readonly NetworkManager _networkManager;
        private readonly GameObject _playerPrefab;

        public PlayerSpawner(NetworkManager networkManager, GameObject playerPrefab)
        {
            _networkManager = networkManager != null
                ? networkManager
                : throw new ArgumentNullException(nameof(networkManager));
            _playerPrefab = playerPrefab != null
                ? playerPrefab
                : throw new ArgumentNullException(nameof(playerPrefab));
        }

        public void SpawnAllConnectedPlayers()
        {
            if (!IsServer())
            {
                return;
            }

            var index = 0;
            foreach (var clientId in _networkManager.ConnectedClientsIds)
            {
                SpawnFor(clientId, index++);
            }
        }

        public void SpawnFor(ulong clientId, int playerIndex)
        {
            if (!IsServer())
            {
                return;
            }

            if (_networkManager.ConnectedClients.TryGetValue(clientId, out var client) &&
                client.PlayerObject != null)
            {
                // Already has an avatar (rejoined, or the scene reloaded).
                return;
            }

            var pose = ResolveSpawnPose(playerIndex);
            var instance = UnityEngine.Object.Instantiate(_playerPrefab, pose.position, pose.rotation);

            if (!instance.TryGetComponent<NetworkObject>(out var networkObject))
            {
                Debug.LogError("The player prefab has no NetworkObject; it cannot be spawned.");
                UnityEngine.Object.Destroy(instance);
                return;
            }

            // SpawnAsPlayerObject ties the avatar's lifetime to the connection:
            // NGO destroys it automatically when that client leaves.
            networkObject.SpawnAsPlayerObject(clientId);
        }

        private (Vector3 position, Quaternion rotation) ResolveSpawnPose(int playerIndex)
        {
            if (AppServices.IsReady &&
                AppServices.Container.TryResolve<ISpawnPointProvider>(out var provider))
            {
                provider.GetSpawnPose(playerIndex, out var position, out var rotation);
                return (position, rotation);
            }

            return (new Vector3(playerIndex * 2f, 1f, 0f), Quaternion.identity);
        }

        private bool IsServer()
        {
            if (_networkManager.IsServer)
            {
                return true;
            }

            Debug.LogWarning("PlayerSpawner was called on a client. Spawning is server-only.");
            return false;
        }
    }
}
