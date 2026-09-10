namespace HoldMyBeer.Networking
{
    /// <summary>
    /// How peers reach each other. No dedicated server in any case: one player
    /// is the host (server + client in the same process).
    /// </summary>
    public enum SessionMode
    {
        /// <summary>LAN / port-forwarded IP. Zero external dependency, ideal to iterate.</summary>
        DirectIp = 0,

        /// <summary>Unity Relay: play over the internet with a join code, no port forwarding.</summary>
        Relay = 1,

        /// <summary>Steam datagram relay: play over the internet, joined through a Steam lobby.</summary>
        Steam = 2
    }
}
