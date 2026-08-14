using CargoClash.Gameplay.Cargo;
using CargoClash.Gameplay.PowerUps;
using CargoClash.Movement;
using UnityEngine;

namespace CargoClash.Gameplay
{
    public sealed class MatchManager : MonoBehaviour
    {
        [Header("Match")]
        [SerializeField, Min(1f)]
        private float matchDuration = 180f;

        [Header("Managers")]
        [SerializeField]
        private MatchScoreManager scoreManager;

        [SerializeField]
        private CargoSpawnManager cargoSpawnManager;

        [SerializeField]
        private PowerUpSpawnManager powerUpSpawnManager;

        [Header("Player")]
        [SerializeField]
        private GameObject player;

        [SerializeField]
        private Vector2Int playerStartCell;

        [Header("Opponent")]
        [SerializeField]
        private GameObject opponent;

        [SerializeField]
        private Vector2Int opponentStartCell;

        private float remainingTime;
        private bool isMatchRunning;

        public float MatchDuration =>
            matchDuration;

        public float RemainingTime =>
            remainingTime;

        public bool IsMatchRunning =>
            isMatchRunning;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void Start()
        {
            StartMatch();
        }

        private void Update()
        {
            if (!isMatchRunning)
            {
                return;
            }

            remainingTime =
                Mathf.Max(
                    0f,
                    remainingTime - Time.deltaTime);

            if (remainingTime <= 0f)
            {
                EndMatch();
            }
        }

        public void StartMatch()
        {
            remainingTime =
                matchDuration;

            isMatchRunning = true;

            SetCharacterGameplayEnabled(
                player,
                true);

            SetCharacterGameplayEnabled(
                opponent,
                true);

            Debug.Log(
                "Match started.",
                this);
        }

        public void EndMatch()
        {
            if (!isMatchRunning)
            {
                return;
            }

            isMatchRunning = false;
            remainingTime = 0f;

            SetCharacterGameplayEnabled(
                player,
                false);

            SetCharacterGameplayEnabled(
                opponent,
                false);

            LogMatchResult();
        }

        public void ResetMatch()
        {
            isMatchRunning = false;

            SetCharacterGameplayEnabled(
                player,
                false);

            SetCharacterGameplayEnabled(
                opponent,
                false);

            scoreManager?.ResetScores();

            cargoSpawnManager?.ResetCargoSystem();
            powerUpSpawnManager?.ResetPowerUps();

            ResetCharacter(
                player,
                playerStartCell);

            ResetCharacter(
                opponent,
                opponentStartCell);

            remainingTime =
                matchDuration;

            SetCharacterGameplayEnabled(
                player,
                true);

            SetCharacterGameplayEnabled(
                opponent,
                true);

            isMatchRunning = true;

            Debug.Log(
                "Match reset.",
                this);
        }

        private void ResetCharacter(
            GameObject character,
            Vector2Int startCell)
        {
            if (character == null)
            {
                return;
            }

            CargoCarrier carrier =
                character.GetComponent<CargoCarrier>();

            CharacterHeatController heat =
                character.GetComponent<CharacterHeatController>();

            CharacterShieldController shield =
                character.GetComponent<CharacterShieldController>();

            CharacterSpeedBoostController speedBoost =
                character.GetComponent<
                    CharacterSpeedBoostController>();

            GridDashController dash =
                character.GetComponent<GridDashController>();

            GridMovementController movement =
                character.GetComponent<
                    GridMovementController>();

            carrier?.ResetCarrier();
            heat?.ResetHeat();
            shield?.ResetShield();
            speedBoost?.ResetSpeedBoost();
            dash?.ResetDashState();

            movement?.ResetToCell(
                startCell);
        }

        private void SetCharacterGameplayEnabled(
            GameObject character,
            bool enabledState)
        {
            if (character == null)
            {
                return;
            }

            GridMovementController movement =
                character.GetComponent<
                    GridMovementController>();

            if (movement != null)
            {
                movement.SetMovementLocked(
                    !enabledState);

                movement.ClearBufferedMovement();
            }

            HumanGridInput humanInput =
                character.GetComponent<
                    HumanGridInput>();

            if (humanInput != null)
            {
                humanInput.enabled =
                    enabledState;
            }

            ScriptedGridBot scriptedBot =
                character.GetComponent<
                    ScriptedGridBot>();

            if (scriptedBot != null)
            {
                scriptedBot.enabled =
                    enabledState;
            }
        }

        private void LogMatchResult()
        {
            if (scoreManager == null)
            {
                return;
            }

            int playerScore =
                scoreManager.PlayerScore;

            int opponentScore =
                scoreManager.OpponentScore;

            if (playerScore > opponentScore)
            {
                Debug.Log(
                    $"Match ended. Player wins " +
                    $"{playerScore}-{opponentScore}.",
                    this);
            }
            else if (opponentScore > playerScore)
            {
                Debug.Log(
                    $"Match ended. Opponent wins " +
                    $"{opponentScore}-{playerScore}.",
                    this);
            }
            else
            {
                Debug.Log(
                    $"Match ended in a draw: " +
                    $"{playerScore}-{opponentScore}.",
                    this);
            }
        }

        private void ResolveDependencies()
        {
            if (scoreManager == null)
            {
                scoreManager =
                    FindAnyObjectByType<
                        MatchScoreManager>();
            }

            if (cargoSpawnManager == null)
            {
                cargoSpawnManager =
                    FindAnyObjectByType<
                        CargoSpawnManager>();
            }

            if (powerUpSpawnManager == null)
            {
                powerUpSpawnManager =
                    FindAnyObjectByType<
                        PowerUpSpawnManager>();
            }
        }

        [ContextMenu("Reset Match")]
        private void DebugResetMatch()
        {
            ResetMatch();
        }
    }
}