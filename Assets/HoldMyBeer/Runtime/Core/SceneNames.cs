namespace HoldMyBeer.Core
{
    /// <summary>
    /// Single source of truth for scene names. Never type a scene name inline:
    /// the editor bootstrap validates the build settings against these constants.
    /// </summary>
    public static class SceneNames
    {
        public const string Boot = "Boot";
        public const string Menu = "Menu";
        public const string Game = "Game";
    }
}
