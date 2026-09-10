using Unity.Netcode;
using UnityEngine;

namespace HoldMyBeer.Networking
{
    /// <summary>Refuses clients once the lobby is full.</summary>
    public sealed class MaxPlayersPolicy : IConnectionApprovalPolicy
    {
        private readonly NetworkManager _networkManager;
        private readonly int _maxPlayers;

        public MaxPlayersPolicy(NetworkManager networkManager, int maxPlayers)
        {
            _networkManager = networkManager;
            _maxPlayers = Mathf.Max(1, maxPlayers);
        }

        public bool Approve(NetworkManager.ConnectionApprovalRequest request, ConnectionPayload payload, out string reason)
        {
            if (_networkManager.ConnectedClientsIds.Count >= _maxPlayers)
            {
                reason = "The session is full.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    /// <summary>
    /// Refuses clients running a different build. Mismatched builds desync in ways
    /// that look like gameplay bugs, so catch it at the door.
    /// </summary>
    public sealed class BuildVersionPolicy : IConnectionApprovalPolicy
    {
        private readonly string _expectedVersion;

        public BuildVersionPolicy(string expectedVersion) => _expectedVersion = expectedVersion;

        public bool Approve(NetworkManager.ConnectionApprovalRequest request, ConnectionPayload payload, out string reason)
        {
            if (!string.Equals(payload.buildVersion, _expectedVersion, System.StringComparison.Ordinal))
            {
                reason = $"Version mismatch: host runs {_expectedVersion}, you run {payload.buildVersion}.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
