namespace HoldMyBeer.UI
{
    /// <summary>One full-screen view. The scene controller shows exactly one at a time.</summary>
    public interface IScreen
    {
        void Build(UnityEngine.Transform root);
        void Show();
        void Hide();
    }
}
