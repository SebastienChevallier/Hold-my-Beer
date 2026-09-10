using HoldMyBeer.Networking;
using Unity.Netcode;

namespace HoldMyBeer.Networking.Relay
{
    /// <summary>
    /// Registers Relay with the bootstrap. Deleting the Relay folder removes the
    /// mode cleanly: nothing else references this type by name.
    /// </summary>
    public sealed class RelayTransportInstaller : ISessionTransportInstaller
    {
        public ISessionTransport Create(NetworkManager networkManager) =>
            new RelaySessionTransport(networkManager);
    }
}
