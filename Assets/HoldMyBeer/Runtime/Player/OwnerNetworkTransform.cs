using Unity.Netcode.Components;

namespace HoldMyBeer.Player
{
    /// <summary>
    /// Owner-authoritative transform: each player simulates their own avatar and
    /// replicates the result. Cheap, responsive, and trivially cheatable — fine for
    /// a game between friends, to be replaced by a server-authoritative motor if
    /// the game ever becomes competitive. See CLAUDE.md, "Authority".
    /// </summary>
    public sealed class OwnerNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
