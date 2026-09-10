using Unity.Netcode.Transports.UTP;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Declares a transport to the bootstrap. Implement it in any assembly and the
    /// mode becomes available: the bootstrap discovers installers by reflection, so
    /// adding (or deleting) a transport never edits the composition root.
    /// </summary>
    public interface ISessionTransportInstaller
    {
        ISessionTransport Create(UnityTransport transport);
    }

    /// <summary>Always available: LAN / direct IP.</summary>
    public sealed class DirectIpTransportInstaller : ISessionTransportInstaller
    {
        public ISessionTransport Create(UnityTransport transport) => new DirectIpSessionTransport(transport);
    }
}
