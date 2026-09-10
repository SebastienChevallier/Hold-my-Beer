using System.Threading.Tasks;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Owns everything specific to one connection mode: which NetworkTransport
    /// component the session runs on, and how it is configured before the session
    /// starts. Adding Steam sockets or a custom relay means adding an implementation
    /// here, never editing <see cref="NetworkSessionService"/> (open/closed).
    ///
    /// An implementation MUST leave NetworkConfig.NetworkTransport pointing at its
    /// own component — see <see cref="NetworkTransportActivator"/> — because the
    /// previous attempt may have left another mode's transport active.
    /// </summary>
    public interface ISessionTransport
    {
        SessionMode Mode { get; }

        /// <summary>Prepares the transport to host. Returns the join code, if the mode has one.</summary>
        Task<SessionResult> ConfigureHostAsync(SessionRequest request);

        /// <summary>Prepares the transport to join an existing session.</summary>
        Task<SessionResult> ConfigureClientAsync(SessionRequest request);
    }
}
