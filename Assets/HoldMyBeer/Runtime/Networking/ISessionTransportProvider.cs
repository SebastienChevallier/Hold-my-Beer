using System.Collections.Generic;

namespace HoldMyBeer.Networking
{
    /// <summary>Resolves the transport strategy registered for a given mode.</summary>
    public interface ISessionTransportProvider
    {
        /// <summary>
        /// The modes actually available in this run, in enum order. The menu cycles
        /// over this rather than over the enum: a mode whose installer bowed out
        /// (Steam not running, Relay folder deleted) must not be offered.
        /// </summary>
        IReadOnlyList<SessionMode> AvailableModes { get; }

        bool TryGet(SessionMode mode, out ISessionTransport transport);
    }
}
