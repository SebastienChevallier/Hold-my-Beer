namespace HoldMyBeer.Networking.Steam
{
    /// <summary>
    /// The Steam application this game identifies as.
    ///
    /// One value, one place. Steam needs it twice - once through the API at startup,
    /// once as a steam_appid.txt file sitting next to the executable - and the two
    /// disagreeing fails in a way that reads like "Steam is not running". So the file
    /// is generated from this constant rather than maintained by hand.
    ///
    /// 480 is Spacewar, Valve's public test app: it exercises P2P sockets and lobbies
    /// without owning a product. Replacing it with a real AppID (Steamworks partner
    /// account, Steam Direct fee) is a one-line change here.
    /// </summary>
    public static class SteamAppId
    {
        public const uint Value = 480;
    }
}
