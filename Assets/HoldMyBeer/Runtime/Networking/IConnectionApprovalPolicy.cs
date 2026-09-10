using Unity.Netcode;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// One rule applied server-side to an incoming connection. Rules compose:
    /// the first one that rejects wins, and the client is told why.
    /// </summary>
    public interface IConnectionApprovalPolicy
    {
        bool Approve(NetworkManager.ConnectionApprovalRequest request, ConnectionPayload payload, out string reason);
    }
}
