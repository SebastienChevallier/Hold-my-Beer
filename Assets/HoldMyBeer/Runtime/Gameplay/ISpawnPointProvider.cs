using UnityEngine;

namespace HoldMyBeer.Gameplay
{
    /// <summary>
    /// Supplies a start position/rotation for a player. Implementations decide the
    /// policy: round-robin markers today, team bases or a random ring tomorrow.
    /// </summary>
    public interface ISpawnPointProvider
    {
        void GetSpawnPose(int playerIndex, out Vector3 position, out Quaternion rotation);
    }
}
