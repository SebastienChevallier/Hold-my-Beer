using System;
using System.Collections.Generic;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// The platform side of "play with a friend": invitations, the friends overlay,
    /// and the presence entry that makes a "Join game" button appear.
    ///
    /// It is deliberately NOT part of <see cref="ISessionTransport"/>. A transport is
    /// a pipe; matchmaking is who you talk to. Keeping them apart is what lets the UI
    /// subscribe to invitations without knowing Steam exists — it only ever sees this
    /// interface, and gets <see cref="NullSessionInviteService"/> when no platform is
    /// available.
    /// </summary>
    public interface ISessionInviteService
    {
        /// <summary>True when a real platform backs this service (Steam running, API up).</summary>
        bool IsAvailable { get; }

        /// <summary>
        /// The player accepted an invitation and wants to join this lobby. Raised on
        /// the Unity thread. The argument feeds <see cref="SessionRequest.JoinSteam"/>.
        /// </summary>
        event Action<ulong> JoinRequested;

        /// <summary>
        /// The friends worth showing in our own picker, most useful first.
        ///
        /// We draw the list ourselves rather than calling the platform's overlay: the
        /// overlay only exists when the platform launched the game, which forces the
        /// player through shortcut gymnastics and cannot be relied on. Empty when no
        /// platform is present.
        /// </summary>
        IReadOnlyList<PlatformFriend> ListInvitableFriends();

        /// <summary>
        /// Sends a platform invitation to the current session. Returns whether the
        /// invitation actually left.
        /// </summary>
        bool Invite(ulong friendId);

        /// <summary>
        /// Steam can launch the game *because* an invitation was accepted, long before
        /// the menu exists to listen. That invitation is held here until the menu is
        /// ready and claims it, exactly once.
        /// </summary>
        bool TryConsumePendingInvite(out ulong lobbyId);

        /// <summary>
        /// Publishes "this player is in a joinable session", which is what makes the
        /// friends list show a Join button without an explicit invitation.
        /// </summary>
        void PublishJoinableSession();

        /// <summary>Removes the presence entry when the session ends.</summary>
        void ClearJoinableSession();
    }
}
