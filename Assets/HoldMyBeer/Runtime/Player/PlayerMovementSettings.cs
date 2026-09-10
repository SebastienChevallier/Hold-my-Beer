using UnityEngine;

namespace HoldMyBeer.Player
{
    /// <summary>Tunable movement values, editable per prefab variant.</summary>
    [System.Serializable]
    public struct PlayerMovementSettings
    {
        [SerializeField] private float walkSpeed;
        [SerializeField] private float sprintSpeed;
        [SerializeField] private float jumpHeight;
        [SerializeField] private float gravity;
        [SerializeField] private float lookSensitivity;
        [SerializeField] private float maxPitch;

        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public float LookSensitivity => lookSensitivity;
        public float MaxPitch => maxPitch;

        public static PlayerMovementSettings Default => new()
        {
            walkSpeed = 5f,
            sprintSpeed = 8f,
            jumpHeight = 1.2f,
            gravity = -19.62f,
            lookSensitivity = 12f,
            maxPitch = 85f
        };
    }
}
