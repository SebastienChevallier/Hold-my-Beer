using Unity.Netcode;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Declares a platform invite service to the bootstrap, exactly like
    /// <see cref="ISessionTransportInstaller"/> does for transports: implement it in
    /// any assembly and the feature appears, delete the assembly and it disappears,
    /// without the composition root ever naming the platform.
    ///
    /// Returning null is legitimate and expected — it means "the platform is not
    /// usable right now" (Steam not running), not "something broke".
    /// </summary>
    public interface ISessionInviteInstaller
    {
        ISessionInviteService Create(NetworkManager networkManager);
    }
}
