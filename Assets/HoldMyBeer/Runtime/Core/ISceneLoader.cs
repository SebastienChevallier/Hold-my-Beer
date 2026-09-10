using System;

namespace HoldMyBeer.Core
{
    /// <summary>
    /// Loads scenes locally, for the client that calls it only.
    /// Scenes that must stay in sync across the session (the Game scene) are loaded
    /// through <c>INetworkSessionService</c> / NetworkSceneManager instead.
    /// </summary>
    public interface ISceneLoader
    {
        string ActiveSceneName { get; }

        void Load(string sceneName, Action onLoaded = null);
    }
}
