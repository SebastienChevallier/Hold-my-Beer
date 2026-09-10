using System;
using Unity.Netcode;
using UnityEngine;
using Steamworks;

namespace HoldMyBeer.Networking.Steam
{
    /// <summary>
    /// Owns the one and only Steam API session for the process.
    ///
    /// This is global state, and §6 says no singletons — the exception is deliberate:
    /// <c>SteamClient</c> wraps a native library that is itself process-global, so
    /// pretending we can hold several instances would be a lie in a nicer costume.
    /// Two installers (transport and invites) are discovered independently and
    /// neither can assume the other ran, so initialisation has to be idempotent
    /// somewhere; here is the honest place for it.
    ///
    /// Failure is expected, not exceptional: Steam simply may not be running. In that
    /// case nothing registers and the game keeps working on the other modes.
    /// </summary>
    internal static class SteamRuntime
    {
        private static bool _attempted;

        internal static bool IsAvailable => SteamClient.IsValid;

        /// <summary>
        /// The lobby the local player is in, shared by the transport and the invite
        /// service: one session means one lobby, so it cannot belong to either alone.
        /// </summary>
        internal static SteamLobbyService Lobby { get; } = new();

        /// <summary>
        /// Brings the Steam API up once, and returns whether it is usable. Safe to
        /// call from every installer.
        /// </summary>
        internal static bool EnsureInitialised(NetworkManager networkManager)
        {
            // Entering play mode with domain reload disabled keeps a live SteamClient
            // while resetting our statics, so trust the API over our own bookkeeping.
            if (SteamClient.IsValid)
            {
                EnsurePump(networkManager);
                return true;
            }

            if (_attempted)
            {
                return false;
            }

            _attempted = true;

            try
            {
                // asyncCallbacks: false — we pump callbacks ourselves, on the Unity
                // thread, so that events reach NGO in a frame it controls.
                SteamClient.Init(SteamAppId.Value, asyncCallbacks: false);
            }
            catch (Exception exception)
            {
                Debug.Log(
                    "[Steam] Steam is not available, the Steam mode stays hidden. " +
                    $"({exception.Message})");
                return false;
            }

            if (!SteamClient.IsValid)
            {
                Debug.Log("[Steam] SteamClient.Init returned an invalid client; Steam mode stays hidden.");
                return false;
            }

            EnsurePump(networkManager);
            Debug.Log($"[Steam] Ready as {SteamClient.Name} ({SteamClient.SteamId.Value}).");
            return true;
        }

        private static void EnsurePump(NetworkManager networkManager)
        {
            if (networkManager == null || networkManager.TryGetComponent<SteamCallbackPump>(out _))
            {
                return;
            }

            // The NetworkManager object survives scene loads, which is exactly the
            // lifetime the Steam API needs.
            networkManager.gameObject.AddComponent<SteamCallbackPump>();
        }

        internal static void Shutdown()
        {
            if (!SteamClient.IsValid)
            {
                return;
            }

            Lobby.Leave();
            SteamClient.Shutdown();
            _attempted = false;
        }
    }
}
