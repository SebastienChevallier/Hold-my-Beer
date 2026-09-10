using Unity.Netcode;
using UnityEngine;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Installs the <see cref="NetworkTransport"/> component a connection mode needs
    /// and makes it the active one.
    ///
    /// Modes do not all share a transport component: Direct IP and Relay both drive
    /// UnityTransport, but Steam sockets are a different component entirely. So a
    /// mode owns its component rather than receiving one, and swaps it in here.
    /// </summary>
    public static class NetworkTransportActivator
    {
        public static TTransport Activate<TTransport>(NetworkManager networkManager)
            where TTransport : NetworkTransport
        {
            if (networkManager == null)
            {
                throw new System.ArgumentNullException(nameof(networkManager));
            }

            if (!networkManager.TryGetComponent<TTransport>(out var transport))
            {
                transport = networkManager.gameObject.AddComponent<TTransport>();
                Debug.Log($"[Transport] Added {typeof(TTransport).Name} to the NetworkManager.");
            }

            networkManager.NetworkConfig.NetworkTransport = transport;
            return transport;
        }
    }
}
