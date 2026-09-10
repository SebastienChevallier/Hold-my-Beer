using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Netcode;
using UnityEngine;
using Steamworks;
using Steamworks.Data;

namespace HoldMyBeer.Networking.Steam
{
    /// <summary>
    /// Carries Netcode traffic over the Steam Datagram Relay instead of raw UDP.
    ///
    /// Written against the NGO 2.x <see cref="NetworkTransport"/> contract rather than
    /// adapted from the community 1.x transports: the pumping hook moved from
    /// MonoBehaviour.Update to <see cref="OnEarlyUpdate"/>, which NGO calls at a point
    /// where it is ready to consume events. Pumping anywhere else reintroduces a
    /// one-frame lag that is miserable to debug.
    ///
    /// Client ids are Steam ids. They are unique, they never collide, and they are
    /// never 0 - which matters, because 0 is reserved for the server.
    /// </summary>
    internal sealed class SteamNetworkTransport : NetworkTransport
    {
        /// <summary>Steam multiplexes sockets per app; any fixed value works as long as both ends agree.</summary>
        private const int VirtualPort = 0;

        private readonly Queue<TransportEvent> _events = new();
        private readonly Dictionary<ulong, Connection> _connections = new();

        private ServerSocket _socket;
        private ClientConnection _client;

        /// <summary>Who the client dials. Set by the session transport before StartClient.</summary>
        internal SteamId TargetSteamId { get; set; }

        public override ulong ServerClientId => 0UL;

        public override bool IsSupported => SteamClient.IsValid;

        public override void Initialize(NetworkManager networkManager = null)
        {
            // Opens the path to Valve's relay network. Doing it early means the first
            // connection does not pay for the handshake.
            SteamNetworkingUtils.InitRelayNetworkAccess();
        }

        public override bool StartServer()
        {
            if (!SteamClient.IsValid)
            {
                Debug.LogError("[Steam] Cannot host: the Steam API is not initialised.");
                return false;
            }

            try
            {
                _socket = SteamNetworkingSockets.CreateRelaySocket<ServerSocket>(VirtualPort);

                // Callbacks only fire from RunCallbacks/Receive, both of which we call
                // later on this thread, so there is no window where Transport is null.
                _socket.Transport = this;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Steam] Could not open the relay socket: {exception.Message}");
                return false;
            }
        }

        public override bool StartClient()
        {
            if (!SteamClient.IsValid)
            {
                Debug.LogError("[Steam] Cannot join: the Steam API is not initialised.");
                return false;
            }

            if (TargetSteamId.Value == 0UL)
            {
                Debug.LogError("[Steam] Cannot join: no host SteamID was provided.");
                return false;
            }

            try
            {
                _client = SteamNetworkingSockets.ConnectRelay<ClientConnection>(TargetSteamId, VirtualPort);
                _client.Transport = this;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Steam] Could not reach host {TargetSteamId.Value}: {exception.Message}");
                return false;
            }
        }

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            var sendType = ToSendType(networkDelivery);

            if (_socket != null)
            {
                if (_connections.TryGetValue(clientId, out var connection))
                {
                    connection.SendMessage(payload.Array, payload.Offset, payload.Count, sendType);
                }

                return;
            }

            // A client only ever talks to the server, so clientId is not consulted.
            if (_client != null && _client.Connected)
            {
                _client.Connection.SendMessage(payload.Array, payload.Offset, payload.Count, sendType);
            }
        }

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            if (_events.Count == 0)
            {
                clientId = 0UL;
                payload = default;
                receiveTime = Time.realtimeSinceStartup;
                return NetworkEvent.Nothing;
            }

            var next = _events.Dequeue();
            clientId = next.ClientId;
            payload = next.Payload;
            receiveTime = next.ReceiveTime;
            return next.Type;
        }

        /// <summary>
        /// Steam hands messages over from inside Receive, so this is where inbound
        /// traffic actually enters the game. NGO calls it right before it drains
        /// <see cref="PollEvent"/>, so a message received here is handled the same frame.
        /// </summary>
        protected override void OnEarlyUpdate()
        {
            try
            {
                _socket?.Receive();
                _client?.Receive();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Steam] Receive failed: {exception.Message}");
            }
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            if (_connections.TryGetValue(clientId, out var connection))
            {
                _connections.Remove(clientId);
                connection.Close();
            }
        }

        public override void DisconnectLocalClient()
        {
            _client?.Close();
            _client = null;
        }

        /// <summary>
        /// Not reported. Steam exposes a ping through connection status, but NGO only
        /// uses this for diagnostics and a wrong number is worse than none.
        /// </summary>
        public override ulong GetCurrentRtt(ulong clientId) => 0UL;

        public override void Shutdown()
        {
            foreach (var connection in _connections.Values)
            {
                connection.Close();
            }

            _connections.Clear();

            _client?.Close();
            _client = null;

            _socket?.Close();
            _socket = null;

            _events.Clear();
            TargetSteamId = default;
        }

        private static SendType ToSendType(NetworkDelivery delivery) => delivery switch
        {
            // Steam's reliable channel is already sequenced and fragmented, so the
            // three reliable flavours collapse onto it.
            NetworkDelivery.Unreliable => SendType.Unreliable,
            NetworkDelivery.UnreliableSequenced => SendType.Unreliable,
            _ => SendType.Reliable
        };

        private void Enqueue(NetworkEvent type, ulong clientId, ArraySegment<byte> payload = default) =>
            _events.Enqueue(new TransportEvent(type, clientId, payload, Time.realtimeSinceStartup));

        /// <summary>
        /// Steam owns the memory behind <paramref name="data"/> and reclaims it as soon
        /// as the callback returns, so the payload has to be copied, not referenced.
        /// </summary>
        private static ArraySegment<byte> CopyPayload(IntPtr data, int size)
        {
            var buffer = new byte[size];
            Marshal.Copy(data, buffer, 0, size);
            return new ArraySegment<byte>(buffer);
        }

        private readonly struct TransportEvent
        {
            internal TransportEvent(NetworkEvent type, ulong clientId, ArraySegment<byte> payload, float receiveTime)
            {
                Type = type;
                ClientId = clientId;
                Payload = payload;
                ReceiveTime = receiveTime;
            }

            internal NetworkEvent Type { get; }
            internal ulong ClientId { get; }
            internal ArraySegment<byte> Payload { get; }
            internal float ReceiveTime { get; }
        }

        /// <summary>
        /// Facepunch instantiates these itself (a new() constraint), so they are wired
        /// to their transport after creation. They stay nested and private: they are
        /// callback plumbing for this transport and have no life without it.
        /// </summary>
        private sealed class ServerSocket : SocketManager
        {
            internal SteamNetworkTransport Transport;

            public override void OnConnecting(Connection connection, ConnectionInfo info)
            {
                // Everyone is let through here; who may actually play is decided by
                // ConnectionApprovalHandler, one layer up.
                connection.Accept();
            }

            public override void OnConnected(Connection connection, ConnectionInfo info)
            {
                var clientId = info.Identity.SteamId.Value;
                Transport._connections[clientId] = connection;
                Transport.Enqueue(NetworkEvent.Connect, clientId);
            }

            public override void OnDisconnected(Connection connection, ConnectionInfo info)
            {
                var clientId = info.Identity.SteamId.Value;
                Transport._connections.Remove(clientId);
                Transport.Enqueue(NetworkEvent.Disconnect, clientId);
            }

            public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size,
                                           long messageNum, long recvTime, int channel)
            {
                Transport.Enqueue(NetworkEvent.Data, identity.SteamId.Value, CopyPayload(data, size));
            }
        }

        private sealed class ClientConnection : ConnectionManager
        {
            internal SteamNetworkTransport Transport;

            public override void OnConnected(ConnectionInfo info) =>
                Transport.Enqueue(NetworkEvent.Connect, Transport.ServerClientId);

            public override void OnDisconnected(ConnectionInfo info) =>
                Transport.Enqueue(NetworkEvent.Disconnect, Transport.ServerClientId);

            public override void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel) =>
                Transport.Enqueue(NetworkEvent.Data, Transport.ServerClientId, CopyPayload(data, size));
        }
    }
}
