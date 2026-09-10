using System;
using System.Collections.Generic;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Used when no platform invite service was discovered (no Steam assembly, or
    /// Steam not running). A null object rather than a null reference: the menu can
    /// call this unconditionally instead of guarding every call site.
    /// </summary>
    public sealed class NullSessionInviteService : ISessionInviteService
    {
        public bool IsAvailable => false;

        /// <summary>Never raised. The accessors exist only to satisfy the contract.</summary>
        public event Action<ulong> JoinRequested
        {
            add { }
            remove { }
        }

        public IReadOnlyList<PlatformFriend> ListInvitableFriends() => Array.Empty<PlatformFriend>();

        public bool Invite(ulong friendId) => false;

        public bool TryConsumePendingInvite(out ulong lobbyId)
        {
            lobbyId = 0UL;
            return false;
        }

        public void PublishJoinableSession()
        {
        }

        public void ClearJoinableSession()
        {
        }
    }
}
