using System.Collections.Generic;

namespace HoldMyBeer.Networking
{
    /// <inheritdoc cref="ISessionTransportProvider"/>
    public sealed class SessionTransportProvider : ISessionTransportProvider
    {
        private readonly Dictionary<SessionMode, ISessionTransport> _transports = new();

        public void Register(ISessionTransport transport)
        {
            if (transport == null)
            {
                return;
            }

            _transports[transport.Mode] = transport;
        }

        public bool TryGet(SessionMode mode, out ISessionTransport transport) =>
            _transports.TryGetValue(mode, out transport);
    }
}
