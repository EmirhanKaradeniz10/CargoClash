using UnityEngine;

namespace CargoClash.Gameplay
{
    public sealed class PlayerIdentity : MonoBehaviour
    {
        [SerializeField]
        private PlayerSide side;

        public PlayerSide Side => side;
    }
}