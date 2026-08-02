using UnityEngine;

namespace CargoClash.Gameplay.Cargo
{
    public sealed class CargoItem : MonoBehaviour
    {
        [Header("Cargo")]
        [SerializeField]
        private CargoType cargoType = CargoType.Normal;

        [SerializeField, Min(0)]
        private int scoreValue = 10;

        private CargoSpawnSlot originSlot;

        public CargoType CargoType => cargoType;

        public int ScoreValue => scoreValue;

        public CargoSpawnSlot OriginSlot => originSlot;

        public void Initialize(CargoSpawnSlot spawnSlot)
        {
            originSlot = spawnSlot;
        }

        public void RemoveFromSlot()
        {
            if (originSlot == null)
            {
                return;
            }

            CargoSpawnSlot previousSlot = originSlot;
            originSlot = null;

            previousSlot.NotifyCargoRemoved(this);
        }
    }
}