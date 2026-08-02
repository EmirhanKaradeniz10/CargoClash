using UnityEngine;

namespace CargoClash.Gameplay
{
    public sealed class MatchScoreManager : MonoBehaviour
    {
        [SerializeField, Min(0)]
        private int playerScore;

        [SerializeField, Min(0)]
        private int opponentScore;

        public int PlayerScore => playerScore;

        public int OpponentScore => opponentScore;

        public void AddScore(PlayerSide side, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            switch (side)
            {
                case PlayerSide.Player:
                    playerScore += amount;
                    break;

                case PlayerSide.Opponent:
                    opponentScore += amount;
                    break;

                default:
                    Debug.LogWarning(
                        $"Unknown player side: {side}",
                        this);
                    return;
            }

            Debug.Log(
                $"Score updated — Player: {playerScore}, " +
                $"Opponent: {opponentScore}",
                this);
        }
    }
}