
using UnityEngine;
using TMPro;

public class PlayerNames3D : MonoBehaviour
{
    [Header("3D Player Name Texts")]
    [SerializeField] private TMP_Text player1NameText;
    [SerializeField] private TMP_Text player2NameText;

    private string lastPlayer1Name = "";
    private string lastPlayer2Name = "";

    private void Start()
    {
        SetNames("Player 1", "Player 2");
    }

    private void Update()
    {
        GamePlayerNames names = GamePlayerNames.Instance;

        if (names == null || !names.IsSpawned)
            return;

        string player1Name = names.GetPlayerName(0);
        string player2Name = names.GetPlayerName(1);

        if (player1Name != lastPlayer1Name ||
            player2Name != lastPlayer2Name)
        {
            SetNames(player1Name, player2Name);
        }
    }

    private void SetNames(string player1, string player2)
    {
        if (player1NameText != null)
            player1NameText.text = player1;

        if (player2NameText != null)
            player2NameText.text = player2;

        lastPlayer1Name = player1;
        lastPlayer2Name = player2;
    }
}
