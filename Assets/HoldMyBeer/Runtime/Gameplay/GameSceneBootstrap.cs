using HoldMyBeer.Core;
using Unity.Netcode;
using UnityEngine;

namespace HoldMyBeer.Gameplay
{
    /// <summary>
    /// Entry point of the Game scene. On the server, spawns every connected player
    /// once the scene finished loading everywhere; on a client, does nothing but
    /// wait for replication.
    /// </summary>
    public sealed class GameSceneBootstrap : MonoBehaviour
    {
        private NetworkManager _networkManager;
        private bool _spawned;

        private void Start()
        {
            _networkManager = NetworkManager.Singleton;

            if (_networkManager == null || !_networkManager.IsListening)
            {
                Debug.LogWarning(
                    "Game scene entered without an active session. " +
                    "Enter play mode from the Boot scene to get a full flow.");
                return;
            }

            if (!_networkManager.IsServer)
            {
                return;
            }

            // The scene event tells us every client is ready to receive spawns.
            // If the load event already fired (host loaded first), spawn right away.
            _networkManager.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
            SpawnOnce();
        }

        private void OnDestroy()
        {
            if (_networkManager != null && _networkManager.SceneManager != null)
            {
                _networkManager.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
            }
        }

        private void HandleLoadEventCompleted(
            string sceneName,
            UnityEngine.SceneManagement.LoadSceneMode loadSceneMode,
            System.Collections.Generic.List<ulong> clientsCompleted,
            System.Collections.Generic.List<ulong> clientsTimedOut)
        {
            if (sceneName == SceneNames.Game)
            {
                SpawnOnce();
            }
        }

        private void SpawnOnce()
        {
            if (_spawned)
            {
                return;
            }

            if (!AppServices.IsReady || !AppServices.Container.TryResolve<IPlayerSpawner>(out var spawner))
            {
                Debug.LogError("No IPlayerSpawner registered. Did the Boot scene run?");
                return;
            }

            _spawned = true;
            spawner.SpawnAllConnectedPlayers();
        }
    }
}
