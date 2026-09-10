namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Who the player is according to the platform they launched from.
    ///
    /// When a platform account exists it is the better answer than anything typed in
    /// a text field: it is the name their friends already recognise, and it cannot be
    /// left blank or spoofed into someone else's.
    ///
    /// Kept apart from <see cref="ISessionInviteService"/> because it answers a
    /// different question - who am I, rather than who do I play with - even though
    /// one platform integration happens to answer both.
    /// </summary>
    public interface IPlatformIdentity
    {
        bool HasIdentity { get; }

        /// <summary>Empty when <see cref="HasIdentity"/> is false.</summary>
        string DisplayName { get; }
    }
}
