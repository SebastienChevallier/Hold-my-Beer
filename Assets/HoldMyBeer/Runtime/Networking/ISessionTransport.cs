using System.Threading.Tasks;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Configures the NetworkTransport for one connection mode, and nothing else.
    /// Adding Steam sockets or a custom relay means adding an implementation here,
    /// never editing <see cref="NetworkSessionService"/> (open/closed).
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
