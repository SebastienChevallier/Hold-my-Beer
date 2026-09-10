using Unity.Netcode;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Declares a transport to the bootstrap. Implement it in any assembly and the
    /// mode becomes available: the bootstrap discovers installers by reflection, so
    /// adding (or deleting) a transport never edits the composition root.
    ///
    /// It receives the NetworkManager, not a transport component: a mode is free to
    /// bring its own (Steam sockets are not a configured UnityTransport, they are a
    /// different component).
    /// </summary>
    public interface ISessionTransportInstaller
    {
        ISessionTransport Create(NetworkManager networkManager);
    }

    /// <summary>Always available: LAN / direct IP.</summary>
    public sealed class DirectIpTransportInstaller : ISessionTransportInstaller
    {
        public ISessionTransport Create(NetworkManager networkManager) =>
            new DirectIpSessionTransport(networkManager);
    }
}
