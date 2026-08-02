using UnityEngine;

namespace CargoClash.Gameplay
{
    public enum PlayerSide
    {
        Player,
        Opponent
    }

    public sealed class BaseZone : MonoBehaviour
    {
        [SerializeField]
        private PlayerSide owner;

        [SerializeField]
        private Vector2Int centerCell;

        [SerializeField, Min(0)]
        private int halfWidth = 1;

        [SerializeField, Min(0)]
        private int halfHeight = 1;

        public PlayerSide Owner => owner;
        public Vector2Int CenterCell => centerCell;

        public bool Contains(Vector2Int cell)
        {
            int minimumX = centerCell.x - halfWidth;
            int maximumX = centerCell.x + halfWidth;
            int minimumY = centerCell.y - halfHeight;
            int maximumY = centerCell.y + halfHeight;

            return cell.x >= minimumX &&
                   cell.x <= maximumX &&
                   cell.y >= minimumY &&
                   cell.y <= maximumY;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = new(
                centerCell.x,
                0.1f,
                centerCell.y);

            Vector3 size = new(
                halfWidth * 2 + 1,
                0.1f,
                halfHeight * 2 + 1);

            Gizmos.DrawWireCube(center, size);
        }
    }
}