
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("Game Text")]
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text player1TotalText;
    [SerializeField] private TMP_Text player2TotalText;
    [SerializeField] private TMP_Text turnText;

    [Header("Score Text")]
    [SerializeField] private TMP_Text player1ScoreText;
    [SerializeField] private TMP_Text player2ScoreText;

    [Header("Mana Text")]
    [SerializeField] private TMP_Text manaText;

    [Header("Target Selection")]
    [SerializeField] private TMP_InputField targetInput;
    [SerializeField] private Button confirmTargetButton;

    [Header("Panel")]
    [SerializeField] private CanvasGroup gamePanelCanvasGroup;

    [Header("Coin Flip")]
    [SerializeField] private Button headsButton;
    [SerializeField] private Button tailsButton;
    [SerializeField] private TMP_Text coinFlipStatusText;

    [SerializeField]
    private float coinResultDisplaySeconds = 3f;

    private int lastSelectionRound = -1;
    private bool targetSubmittedLocally;

    private int lastCoinResultRound = -1;
    private float coinResultHideAt = -1f;

    private BoardGameManager gameManager;

    private void Start()
    {
        gameManager = BoardGameManager.Instance;

        if (confirmTargetButton != null)
            confirmTargetButton.onClick.AddListener(ConfirmTarget);

        if (headsButton != null)
            headsButton.onClick.AddListener(ChooseHeads);

        if (tailsButton != null)
            tailsButton.onClick.AddListener(ChooseTails);

        HideGamePanel();
    }

    private void Update()
    {
        if (BoardGameManager.Instance == null)
        {
            HideGamePanel();
            return;
        }

        gameManager = BoardGameManager.Instance;

        if (!gameManager.GameStarted.Value)
        {
            HideGamePanel();
            return;
        }

        ShowGamePanel();

        UpdateGameTexts();
        UpdateScores();
        UpdateMana();
        UpdateTargetSelection();
    }

    // =====================================
    // PLAYER NAMES
    // =====================================

    private string GetPlayerName(int playerIndex)
    {
        GamePlayerNames names = GamePlayerNames.Instance;

        if (names != null && names.IsSpawned)
        {
            string name = names.GetPlayerName(playerIndex);

            if (!string.IsNullOrWhiteSpace(name))
                return name;
        }

        return $"Player {playerIndex + 1}";
    }

    // =====================================
    // GAME TEXT
    // =====================================

    private void UpdateGameTexts()
    {
        if (roundText != null)
        {
            roundText.text =
                $"Round: {gameManager.CurrentRound.Value} / 8";
        }

        int localPlayer = gameManager.GetLocalPlayerNumber();

        if (player1TotalText != null)
        {
            int displayedP1Total =
                localPlayer == 0
                    ? gameManager.Player1Total.Value
                    : gameManager.Player1VisibleTotal.Value;

            player1TotalText.text =
                $"Player 1 Total: {displayedP1Total}";
        }

        if (player2TotalText != null)
        {
            int displayedP2Total =
                localPlayer == 1
                    ? gameManager.Player2Total.Value
                    : gameManager.Player2VisibleTotal.Value;

            player2TotalText.text =
                $"Player 2 Total: {displayedP2Total}";
        }

        if (targetText != null)
        {
            targetText.text = gameManager.TargetSelected.Value
                ? $"Target: {gameManager.TargetNumber.Value}"
                : "Target: ?";
        }
    }

    // =====================================
    // SCORES WITH PLAYER NAMES
    // =====================================

    private void UpdateScores()
    {
        string player1Name = GetPlayerName(0);
        string player2Name = GetPlayerName(1);

        if (player1ScoreText != null)
        {
            player1ScoreText.text =
                $"{player1Name} - Score: {gameManager.Player1Score.Value}";
        }

        if (player2ScoreText != null)
        {
            player2ScoreText.text =
                $"{player2Name} - Score: {gameManager.Player2Score.Value}";
        }
    }

    // =====================================
    // MANA
    // =====================================

    private void UpdateMana()
    {
        if (manaText == null)
            return;

        int localPlayer = gameManager.GetLocalPlayerNumber();

        if (localPlayer == 0)
        {
            manaText.text =
                $"Mana: {gameManager.Player1Mana.Value}";
        }
        else if (localPlayer == 1)
        {
            manaText.text =
                $"Mana: {gameManager.Player2Mana.Value}";
        }
        else
        {
            manaText.text = "Mana: 0";
        }
    }

    // =====================================
    // TARGET / ROUND / GAME UI
    // =====================================

    private void UpdateTargetSelection()
    {
        // FINAL GAME RESULT

        if (gameManager.GameFinished.Value)
        {
            HideTargetControls();
            ShowCoinControls(false);

            int winner = gameManager.FinalWinner.Value;

            if (turnText != null)
            {
                if (winner == 2)
                {
                    turnText.text =
                        "GAME FINISHED!\nFINAL RESULT: DRAW";
                }
                else if (winner == 0 || winner == 1)
                {
                    turnText.text =
                        "GAME FINISHED!\n" +
                        $"PLAYER {winner + 1} WINS!";
                }
                else
                {
                    turnText.text = "GAME FINISHED!";
                }
            }

            return;
        }

        // ROUND RESULT

        if (gameManager.RoundEnded.Value)
        {
            HideTargetControls();
            ShowCoinControls(false);

            int winner = gameManager.RoundWinner.Value;

            if (turnText != null)
            {
                if (winner == 2)
                {
                    turnText.text =
                        "ROUND ENDED!\n" +
                        "TIE! +5 POINTS EACH";
                }
                else if (winner == 0 || winner == 1)
                {
                    turnText.text =
                        "ROUND ENDED!\n" +
                        $"PLAYER {winner + 1} WINS! +10";
                }
                else
                {
                    turnText.text = "ROUND ENDED!";
                }
            }

            return;
        }

        // COIN FLIP / TARGET PROPOSALS

        if (!gameManager.TargetSelected.Value)
        {
            int round = gameManager.CurrentRound.Value;

            if (lastSelectionRound != round)
            {
                lastSelectionRound = round;
                targetSubmittedLocally = false;

                if (targetInput != null)
                    targetInput.text = "";
            }

            int player = gameManager.GetLocalPlayerNumber();

            bool myReady =
                player == 0
                    ? gameManager.Player1Ready.Value
                    : player == 1 &&
                      gameManager.Player2Ready.Value;

            bool bothReady =
                gameManager.Player1Ready.Value &&
                gameManager.Player2Ready.Value;

            int p1Side = gameManager.Player1CoinSide.Value;

            if (turnText != null)
            {
                if (player < 0)
                {
                    turnText.text =
                        "Waiting for player assignment...";
                }
                else if (myReady)
                {
                    turnText.text =
                        "Target confirmed. Waiting for coin flip...";
                }
                else
                {
                    turnText.text =
                        "Choose a target (35-50) and confirm.";
                }
            }

            ShowTargetControls();

            if (targetInput != null)
            {
                targetInput.interactable =
                    !myReady && !targetSubmittedLocally;
            }

            if (confirmTargetButton != null)
            {
                confirmTargetButton.interactable =
                    player >= 0 &&
                    !myReady &&
                    !targetSubmittedLocally;
            }

            ShowCoinControls(
                player == 0 && p1Side == -1
            );

            if (coinFlipStatusText != null)
            {
                if (p1Side == -1)
                {
                    coinFlipStatusText.text =
                        player == 0
                            ? "Player 1: choose Heads or Tails"
                            : "Waiting for Player 1 to choose a coin side...";
                }
                else
                {
                    string mine =
                        (player == 0 ? p1Side : 1 - p1Side) == 0
                            ? "Heads"
                            : "Tails";

                    coinFlipStatusText.text =
                        "Your side: " + mine + " | " +
                        (bothReady
                            ? "Flipping..."
                            : "Waiting for both targets...");
                }
            }

            return;
        }

        // COIN RESULT TIMER

        if (gameManager.CoinResult.Value != -1 &&
            lastCoinResultRound != gameManager.CurrentRound.Value)
        {
            lastCoinResultRound =
                gameManager.CurrentRound.Value;

            coinResultHideAt =
                Time.time +
                Mathf.Max(0f, coinResultDisplaySeconds);
        }

        // NORMAL TURN

        HideTargetControls();
        ShowCoinControls(false);

        if (coinFlipStatusText != null &&
            gameManager.CoinResult.Value != -1 &&
            Time.time < coinResultHideAt)
        {
            string side =
                gameManager.CoinResult.Value == 0
                    ? "Heads"
                    : "Tails";

            coinFlipStatusText.text =
                "Coin: " + side +
                " | Player " +
                (gameManager.TargetChooser.Value + 1) +
                " won | Target: " +
                gameManager.TargetNumber.Value;
        }
        else if (coinFlipStatusText != null)
        {
            coinFlipStatusText.text = "";
        }

        int localPlayer = gameManager.GetLocalPlayerNumber();
        int currentTurn = gameManager.CurrentTurn.Value;

        if (turnText == null)
            return;

        if (currentTurn == 0)
        {
            turnText.text =
                localPlayer == 0
                    ? "Your Turn - Player 1"
                    : "Player 1 Turn";
        }
        else if (currentTurn == 1)
        {
            turnText.text =
                localPlayer == 1
                    ? "Your Turn - Player 2"
                    : "Player 2 Turn";
        }
        else
        {
            turnText.text = "Waiting...";
        }
    }

    // =====================================
    // TARGET CONTROLS
    // =====================================

    private void ShowTargetControls()
    {
        if (targetInput != null &&
            !targetInput.gameObject.activeSelf)
        {
            targetInput.text = "";
            targetInput.gameObject.SetActive(true);
        }

        if (confirmTargetButton != null)
        {
            confirmTargetButton.gameObject.SetActive(true);
        }
    }

    private void HideTargetControls()
    {
        if (targetInput != null)
        {
            targetInput.gameObject.SetActive(false);
        }

        if (confirmTargetButton != null)
        {
            confirmTargetButton.gameObject.SetActive(false);
        }
    }

    // =====================================
    // TARGET CONFIRM
    // =====================================

    private void ConfirmTarget()
    {
        if (gameManager == null)
            return;

        if (!gameManager.GameStarted.Value)
            return;

        if (gameManager.GameFinished.Value ||
            gameManager.RoundEnded.Value ||
            gameManager.TargetSelected.Value)
        {
            return;
        }

        int localPlayer = gameManager.GetLocalPlayerNumber();

        if (localPlayer < 0 ||
            targetSubmittedLocally ||
            (localPlayer == 0
                ? gameManager.Player1Ready.Value
                : gameManager.Player2Ready.Value))
        {
            return;
        }

        if (targetInput == null)
            return;

        if (!int.TryParse(targetInput.text, out int target))
        {
            Debug.LogWarning("Enter a valid number.");
            return;
        }

        if (target < 35 || target > 50)
        {
            Debug.LogWarning(
                "Target must be between 35 and 50."
            );
            return;
        }

        Debug.Log("Submitting Target: " + target);

        targetSubmittedLocally = true;
        gameManager.SubmitTargetRpc(target);
    }

    // =====================================
    // COIN FLIP
    // =====================================

    private void ChooseHeads()
    {
        ChooseCoinSide(0);
    }

    private void ChooseTails()
    {
        ChooseCoinSide(1);
    }

    private void ChooseCoinSide(int side)
    {
        if (gameManager == null ||
            !gameManager.GameStarted.Value ||
            gameManager.TargetSelected.Value ||
            gameManager.RoundEnded.Value ||
            gameManager.GameFinished.Value ||
            gameManager.GetLocalPlayerNumber() != 0 ||
            gameManager.Player1CoinSide.Value != -1)
        {
            return;
        }

        gameManager.ChooseCoinSideRpc(side);
        ShowCoinControls(false);
    }

    private void ShowCoinControls(bool show)
    {
        if (headsButton != null)
            headsButton.gameObject.SetActive(show);

        if (tailsButton != null)
            tailsButton.gameObject.SetActive(show);
    }

    // =====================================
    // PANEL
    // =====================================

    private void HideGamePanel()
    {
        ShowCoinControls(false);

        if (gamePanelCanvasGroup == null)
            return;

        gamePanelCanvasGroup.alpha = 0f;
        gamePanelCanvasGroup.interactable = false;
        gamePanelCanvasGroup.blocksRaycasts = false;
    }

    private void ShowGamePanel()
    {
        if (gamePanelCanvasGroup == null)
            return;

        gamePanelCanvasGroup.alpha = 1f;
        gamePanelCanvasGroup.interactable = true;
        gamePanelCanvasGroup.blocksRaycasts = true;
    }

    // =====================================
    // CLEANUP
    // =====================================

    private void OnDestroy()
    {
        if (headsButton != null)
            headsButton.onClick.RemoveListener(ChooseHeads);

        if (tailsButton != null)
            tailsButton.onClick.RemoveListener(ChooseTails);

        if (confirmTargetButton != null)
            confirmTargetButton.onClick.RemoveListener(ConfirmTarget);
    }
}
