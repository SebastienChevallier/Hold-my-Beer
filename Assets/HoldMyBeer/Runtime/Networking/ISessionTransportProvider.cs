namespace HoldMyBeer.Networking
{
    /// <summary>Resolves the transport strategy registered for a given mode.</summary>
    public interface ISessionTransportProvider
    {
        bool TryGet(SessionMode mode, out ISessionTransport transport);
    }
}
