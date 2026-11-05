
using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject gameOverPanel;
    public GameObject gamePanel;

    [Header("Result Texts")]
    public TMP_Text player1FinalScoreText;
    public TMP_Text player2FinalScoreText;
    public TMP_Text winnerText;

    private BoardGameManager gameManager;
    private bool resultShown;

    private void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Update()
    {
        if (resultShown)
            return;

        if (gameManager == null)
            gameManager = BoardGameManager.Instance;

        if (gameManager == null ||
            !gameManager.IsSpawned ||
            !gameManager.GameFinished.Value)
            return;

        ShowFinalResult();
    }

    private void ShowFinalResult()
    {
        int p1Score = gameManager.Player1Score.Value;
        int p2Score = gameManager.Player2Score.Value;
        int winner = gameManager.FinalWinner.Value;

        if (player1FinalScoreText != null)
            player1FinalScoreText.text =
                "Player 1: " + p1Score;

        if (player2FinalScoreText != null)
            player2FinalScoreText.text =
                "Player 2: " + p2Score;

        if (winnerText != null)
        {
            switch (winner)
            {
                case 0:
                    winnerText.text = "PLAYER 1 WINS!";
                    break;

                case 1:
                    winnerText.text = "PLAYER 2 WINS!";
                    break;

                case 2:
                    winnerText.text = "DRAW!";
                    break;

                default:
                    return;
            }
        }

        resultShown = true;

        // Hide gameplay UI.
        if (gamePanel != null)
            gamePanel.SetActive(false);

        // Show final result UI.
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Debug.Log("[GameOverUI] Final result displayed.");
    }
}
