using System;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Plain UDP over Unity Transport. Works on a LAN out of the box, and over the
    /// internet if the host forwards the port. No account, no service, no quota:
    /// this is the mode to develop against.
    /// </summary>
    public sealed class DirectIpSessionTransport : ISessionTransport
    {
        private const string ListenOnAllInterfaces = "0.0.0.0";

        private readonly UnityTransport _transport;

        public DirectIpSessionTransport(UnityTransport transport)
        {
            _transport = transport != null ? transport : throw new ArgumentNullException(nameof(transport));
        }

        public SessionMode Mode => SessionMode.DirectIp;

        public Task<SessionResult> ConfigureHostAsync(SessionRequest request)
        {
            _transport.SetConnectionData(SessionRequest.LoopbackAddress, request.Port, ListenOnAllInterfaces);
            return Task.FromResult(SessionResult.Ok());
        }

        public Task<SessionResult> ConfigureClientAsync(SessionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Address))
            {
                return Task.FromResult(SessionResult.Fail("Enter the host's IP address."));
            }

            _transport.SetConnectionData(request.Address, request.Port);
            return Task.FromResult(SessionResult.Ok());
        }
    }
}
