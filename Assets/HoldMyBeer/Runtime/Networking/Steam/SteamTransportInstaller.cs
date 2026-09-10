using Unity.Netcode;

namespace HoldMyBeer.Networking.Steam
{
    /// <summary>
    /// Registers the Steam mode, but only when Steam is actually there. Returning
    /// null is the normal outcome in the editor without Steam running: the mode
    /// simply never appears in the menu, and Direct IP keeps working.
    /// </summary>
    public sealed class SteamTransportInstaller : ISessionTransportInstaller
    {
        public ISessionTransport Create(NetworkManager networkManager) =>
            SteamRuntime.EnsureInitialised(networkManager)
                ? new SteamSessionTransport(networkManager)
                : null;
    }
}
