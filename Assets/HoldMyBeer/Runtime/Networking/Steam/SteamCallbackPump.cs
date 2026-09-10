using UnityEngine;
using Steamworks;

namespace HoldMyBeer.Networking.Steam
{
    /// <summary>
    /// Drives <c>SteamClient.RunCallbacks()</c> once per frame.
    ///
    /// Steam delivers everything — lobby entered, invitation accepted, connection
    /// state changes — from inside this call, so nothing Steam-related happens on a
    /// frame where it does not run. It ticks on the NetworkManager object because
    /// that object outlives scene loads, and Steam must keep running while the Game
    /// scene loads.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    internal sealed class SteamCallbackPump : MonoBehaviour
    {
        private void Update()
        {
            if (SteamClient.IsValid)
            {
                SteamClient.RunCallbacks();
            }
        }

        private void OnApplicationQuit() => SteamRuntime.Shutdown();
    }
}
