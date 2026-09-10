using HoldMyBeer.Core;
using UnityEngine;

namespace HoldMyBeer.Gameplay
{
    /// <summary>
    /// Scene-authored spawn markers. Drop this on an empty object in the Game scene
    /// and give it children; each child is a spawn pose, assigned round-robin.
    /// Falls back to a circle around the origin when the scene has no markers.
    /// </summary>
    public sealed class SpawnPointRegistry : MonoBehaviour, ISpawnPointProvider
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float fallbackRadius = 4f;

        private void Awake()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                spawnPoints = CollectChildren();
            }

            if (AppServices.IsReady)
            {
                AppServices.Container.Register<ISpawnPointProvider>(this);
            }
        }

        private void OnDestroy()
        {
            if (AppServices.IsReady &&
                AppServices.Container.TryResolve<ISpawnPointProvider>(out var current) &&
                ReferenceEquals(current, this))
            {
                AppServices.Container.Unregister<ISpawnPointProvider>();
            }
        }

        public void GetSpawnPose(int playerIndex, out Vector3 position, out Quaternion rotation)
        {
            if (spawnPoints is { Length: > 0 })
            {
                var point = spawnPoints[playerIndex % spawnPoints.Length];
                position = point.position;
                rotation = point.rotation;
                return;
            }

            var angle = playerIndex * 60f * Mathf.Deg2Rad;
            position = new Vector3(Mathf.Cos(angle) * fallbackRadius, 1f, Mathf.Sin(angle) * fallbackRadius);
            rotation = Quaternion.LookRotation(new Vector3(-position.x, 0f, -position.z).normalized, Vector3.up);
        }

        private Transform[] CollectChildren()
        {
            var children = new Transform[transform.childCount];
            for (var i = 0; i < children.Length; i++)
            {
                children[i] = transform.GetChild(i);
            }

            return children;
        }
    }
}
