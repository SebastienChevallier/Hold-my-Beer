using UnityEngine;

namespace HoldMyBeer.Core
{
    /// <summary>
    /// Access point to the container built by the Boot scene.
    /// This is the ONLY static in the project: everything else receives its
    /// dependencies through constructors or <see cref="IServiceContainer"/> lookups
    /// performed once, in <c>Awake</c>. Do not call <see cref="Container"/> per frame.
    /// </summary>
    public static class AppServices
    {
        private static IServiceContainer _container;

        public static bool IsReady => _container != null;

        public static IServiceContainer Container
        {
            get
            {
                if (_container == null)
                {
                    Debug.LogError(
                        "AppServices used before the Boot scene ran. " +
                        "Always enter play mode from the Boot scene (Tools > Hold My Beer > Play From Boot).");
                }

                return _container;
            }
        }

        /// <summary>Called by the Boot scene's composition root. Nowhere else.</summary>
        public static void SetContainer(IServiceContainer container) => _container = container;

        /// <summary>Called when the composition root is torn down. Nowhere else.</summary>
        public static void Clear() => _container = null;
    }
}
