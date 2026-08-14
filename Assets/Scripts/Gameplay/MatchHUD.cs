using CargoClash.Gameplay;
using TMPro;
using UnityEngine;

namespace CargoClash.UI
{
    public sealed class MatchHUD : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private MatchManager matchManager;

        [SerializeField]
        private MatchScoreManager scoreManager;

        [Header("UI")]
        [SerializeField]
        private TMP_Text playerScoreText;

        [SerializeField]
        private TMP_Text timerText;

        [SerializeField]
        private TMP_Text opponentScoreText;

        private void Awake()
        {
            if (matchManager == null)
            {
                matchManager =
                    FindAnyObjectByType<MatchManager>();
            }

            if (scoreManager == null)
            {
                scoreManager =
                    FindAnyObjectByType<MatchScoreManager>();
            }
        }

        private void Update()
        {
            UpdateTimer();
            UpdateScores();
        }

        private void UpdateTimer()
        {
            if (matchManager == null ||
                timerText == null)
            {
                return;
            }

            float remainingTime =
                matchManager.RemainingTime;

            int totalSeconds =
                Mathf.CeilToInt(remainingTime);

            int minutes =
                totalSeconds / 60;

            int seconds =
                totalSeconds % 60;

            timerText.text =
                $"{minutes:00}:{seconds:00}";
        }

        private void UpdateScores()
        {
            if (scoreManager == null)
            {
                return;
            }

            if (playerScoreText != null)
            {
                playerScoreText.text =
                    scoreManager.PlayerScore.ToString();
            }

            if (opponentScoreText != null)
            {
                opponentScoreText.text =
                    scoreManager.OpponentScore.ToString();
            }
        }
    }
}