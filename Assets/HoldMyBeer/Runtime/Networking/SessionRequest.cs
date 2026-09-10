namespace HoldMyBeer.Networking
{
    /// <summary>Everything a transport needs to open or join a session.</summary>
    public readonly struct SessionRequest
    {
        public const ushort DefaultPort = 7777;
        public const string LoopbackAddress = "127.0.0.1";

        public SessionMode Mode { get; }

        /// <summary>Direct IP only. Ignored by Relay.</summary>
        public string Address { get; }

        /// <summary>Direct IP only. Ignored by Relay.</summary>
        public ushort Port { get; }

        /// <summary>Relay only, when joining. Ignored by Direct IP.</summary>
        public string JoinCode { get; }

        /// <summary>Host only. Includes the host itself.</summary>
        public int MaxPlayers { get; }

        private SessionRequest(SessionMode mode, string address, ushort port, string joinCode, int maxPlayers)
        {
            Mode = mode;
            Address = address;
            Port = port;
            JoinCode = joinCode;
            MaxPlayers = maxPlayers;
        }

        public static SessionRequest HostDirectIp(int maxPlayers, ushort port = DefaultPort) =>
            new(SessionMode.DirectIp, "0.0.0.0", port, string.Empty, maxPlayers);

        public static SessionRequest JoinDirectIp(string address, ushort port = DefaultPort) =>
            new(SessionMode.DirectIp, string.IsNullOrWhiteSpace(address) ? LoopbackAddress : address.Trim(),
                port, string.Empty, 0);

        public static SessionRequest HostRelay(int maxPlayers) =>
            new(SessionMode.Relay, string.Empty, 0, string.Empty, maxPlayers);

        public static SessionRequest JoinRelay(string joinCode) =>
            new(SessionMode.Relay, string.Empty, 0, (joinCode ?? string.Empty).Trim().ToUpperInvariant(), 0);
    }
}
