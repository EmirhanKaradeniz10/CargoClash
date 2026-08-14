using UnityEngine;

namespace CargoClash.Gameplay.PowerUps
{
    public sealed class CharacterShieldController : MonoBehaviour
    {
        [Header("Shield")]
        [SerializeField]
        private bool startsWithShield;

        private bool hasShield;

        public bool HasShield => hasShield;

        private void Awake()
        {
            hasShield = startsWithShield;
        }

        public bool TryConsumeShield()
        {
            if (!hasShield)
            {
                return false;
            }

            hasShield = false;

            Debug.Log(
                $"{name} consumed its shield.",
                this);

            return true;
        }

        public void ResetShield()
        {
            hasShield = false;
        }

        public bool TryGiveShield()
        {
            if (hasShield)
            {
                return false;
            }

            hasShield = true;

            Debug.Log(
                $"{name} received a shield.",
                this);

            return true;
        }

        public void RemoveShield()
        {
            hasShield = false;
        }
    }
}