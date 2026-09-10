using System;
using Unity.Collections;
using Unity.Netcode;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Replicated row of the lobby list. Must stay unmanaged (no string, no class)
    /// to be usable inside a <see cref="NetworkList{T}"/>.
    /// </summary>
    public struct LobbyPlayer : INetworkSerializable, IEquatable<LobbyPlayer>
    {
        public ulong ClientId;
        public FixedString32Bytes DisplayName;
        public bool IsReady;
        public bool IsHost;

        public LobbyPlayer(ulong clientId, string displayName, bool isHost)
        {
            ClientId = clientId;
            DisplayName = new FixedString32Bytes(Truncate(displayName));
            IsReady = false;
            IsHost = isHost;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref DisplayName);
            serializer.SerializeValue(ref IsReady);
            serializer.SerializeValue(ref IsHost);
        }

        public bool Equals(LobbyPlayer other) =>
            ClientId == other.ClientId &&
            DisplayName.Equals(other.DisplayName) &&
            IsReady == other.IsReady &&
            IsHost == other.IsHost;

        public override bool Equals(object obj) => obj is LobbyPlayer other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(ClientId, DisplayName, IsReady, IsHost);

        // FixedString32Bytes holds 29 bytes of UTF-8 payload; overflowing it throws.
        private static string Truncate(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Player";
            }

            const int maxBytes = 29;
            var bytes = System.Text.Encoding.UTF8.GetByteCount(value);
            while (bytes > maxBytes && value.Length > 1)
            {
                value = value[..^1];
                bytes = System.Text.Encoding.UTF8.GetByteCount(value);
            }

            return value;
        }
    }
}
