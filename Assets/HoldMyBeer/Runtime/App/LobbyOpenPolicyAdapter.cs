using HoldMyBeer.Networking;
using Unity.Netcode;

namespace HoldMyBeer.App
{
    /// <summary>
    /// The lobby object does not exist yet when the approval policies are built, so
    /// this adapter resolves it lazily through the provider on each connection.
    /// </summary>
    public sealed class LobbyOpenPolicyAdapter : IConnectionApprovalPolicy
    {
        private readonly ILobbyProvider _provider;

        public LobbyOpenPolicyAdapter(ILobbyProvider provider) => _provider = provider;

        public bool Approve(NetworkManager.ConnectionApprovalRequest request, ConnectionPayload payload,
                            out string reason)
        {
            var lobby = _provider.Current;
            if (lobby != null && !lobby.IsOpen)
            {
                reason = "The match has already started.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
