using UnityEngine;

namespace CargoClash.Gameplay.PowerUps
{
    public sealed class PowerUpSpawnSlot : MonoBehaviour
    {
        [SerializeField]
        private Transform spawnPoint;

        public Vector3 SpawnPosition =>
            spawnPoint != null
                ? spawnPoint.position
                : transform.position;

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(
                SpawnPosition,
                0.35f);
        }
    }
}