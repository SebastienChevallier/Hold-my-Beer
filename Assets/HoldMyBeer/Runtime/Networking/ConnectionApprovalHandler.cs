using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Server-side gatekeeper. Runs every registered <see cref="IConnectionApprovalPolicy"/>
    /// and, on approval, records the sanitized display name for the lobby to publish.
    /// </summary>
    public sealed class ConnectionApprovalHandler : IDisposable
    {
        private readonly NetworkManager _networkManager;
        private readonly List<IConnectionApprovalPolicy> _policies = new();
        private readonly Dictionary<ulong, string> _approvedNames = new();

        public ConnectionApprovalHandler(NetworkManager networkManager)
        {
            _networkManager = networkManager != null
                ? networkManager
                : throw new ArgumentNullException(nameof(networkManager));

            _networkManager.NetworkConfig.ConnectionApproval = true;
            _networkManager.ConnectionApprovalCallback += Handle;
            _networkManager.OnClientDisconnectCallback += Forget;
        }

        public void AddPolicy(IConnectionApprovalPolicy policy)
        {
            if (policy != null)
            {
                _policies.Add(policy);
            }
        }

        /// <summary>Server-side truth for a client's name, set at approval time.</summary>
        public string GetDisplayName(ulong clientId) =>
            _approvedNames.TryGetValue(clientId, out var name) ? name : "Player";

        public void Dispose()
        {
            if (_networkManager == null)
            {
                return;
            }

            _networkManager.ConnectionApprovalCallback -= Handle;
            _networkManager.OnClientDisconnectCallback -= Forget;
            _approvedNames.Clear();
        }

        private void Handle(NetworkManager.ConnectionApprovalRequest request,
                            NetworkManager.ConnectionApprovalResponse response)
        {
            if (!ConnectionPayload.TryDeserialize(request.Payload, out var payload))
            {
                Reject(response, "Invalid connection payload.");
                return;
            }

            foreach (var policy in _policies)
            {
                if (policy.Approve(request, payload, out var reason))
                {
                    continue;
                }

                Reject(response, reason);
                return;
            }

            _approvedNames[request.ClientNetworkId] = Core.PlayerPrefsPlayerProfile.Sanitize(payload.displayName);

            response.Approved = true;

            // The player object is spawned by the game scene, not on connection:
            // players stay avatar-less in the lobby.
            response.CreatePlayerObject = false;
        }

        private static void Reject(NetworkManager.ConnectionApprovalResponse response, string reason)
        {
            Debug.Log($"[Approval] Rejected: {reason}");
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Reason = reason;
        }

        private void Forget(ulong clientId) => _approvedNames.Remove(clientId);
    }
}
