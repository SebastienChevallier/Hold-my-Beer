using System.Collections.Generic;

namespace HoldMyBeer.Networking
{
    /// <inheritdoc cref="ISessionTransportProvider"/>
    public sealed class SessionTransportProvider : ISessionTransportProvider
    {
        private readonly Dictionary<SessionMode, ISessionTransport> _transports = new();
        private readonly List<SessionMode> _modes = new();

        public IReadOnlyList<SessionMode> AvailableModes => _modes;

        public void Register(ISessionTransport transport)
        {
            if (transport == null)
            {
                return;
            }

            if (!_transports.ContainsKey(transport.Mode))
            {
                _modes.Add(transport.Mode);

                // Reflection gives no ordering guarantee, and the menu cycles through
                // this list: sort so the mode order stays the same between runs.
                _modes.Sort();
            }

            _transports[transport.Mode] = transport;
        }

        public bool TryGet(SessionMode mode, out ISessionTransport transport) =>
            _transports.TryGetValue(mode, out transport);
    }
}
