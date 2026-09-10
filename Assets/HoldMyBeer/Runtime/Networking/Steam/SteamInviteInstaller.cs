using Unity.Netcode;

namespace HoldMyBeer.Networking.Steam
{
    /// <summary>
    /// Registers Steam invitations. Discovered independently of
    /// <see cref="SteamTransportInstaller"/>, so it initialises the Steam API itself
    /// rather than assuming the other installer ran first.
    /// </summary>
    public sealed class SteamInviteInstaller : ISessionInviteInstaller
    {
        public ISessionInviteService Create(NetworkManager networkManager) =>
            SteamRuntime.EnsureInitialised(networkManager)
                ? new SteamInviteService(networkManager)
                : null;
    }
}
