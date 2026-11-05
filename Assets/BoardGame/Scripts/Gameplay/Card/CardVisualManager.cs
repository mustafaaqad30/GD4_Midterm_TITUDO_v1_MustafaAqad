
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CardVisualManager : MonoBehaviour
{
    [Header("Card Prefab")]
    [SerializeField] private GameObject cardPrefab;

    [Header("Card Points")]
    [Tooltip("The point near Player 1 seat.")]
    [SerializeField] private Transform player1CardsPoint;

    [Tooltip("The point near Player 2 seat.")]
    [SerializeField] private Transform player2CardsPoint;

    [Header("Card Layout")]
    [SerializeField] private float cardSpacing = 1.2f;

    private readonly List<GameObject> player1VisualCards =
        new List<GameObject>();

    private readonly List<GameObject> player2VisualCards =
        new List<GameObject>();

    private bool startingCardsCreated = false;

    private int previousP1Count = 0;
    private int previousP2Count = 0;

    private void Update()
    {
        if (BoardGameManager.Instance == null)
            return;

        BoardGameManager game = BoardGameManager.Instance;

        if (!game.GameStarted.Value)
            return;

        // New round / waiting for target
        if (!game.TargetSelected.Value)
        {
            if (startingCardsCreated)
                ClearAllCards();

            startingCardsCreated = false;
            previousP1Count = 0;
            previousP2Count = 0;

            return;
        }

        int localPlayer = game.GetLocalPlayerNumber();

        if (localPlayer == -1)
            return;

        // Create starting cards
        if (!startingCardsCreated)
        {
            if (game.Player1CardCount.Value < 2 ||
                game.Player2CardCount.Value < 2)
                return;

            CreateStartingCards(game);

            previousP1Count = game.Player1CardCount.Value;
            previousP2Count = game.Player2CardCount.Value;

            startingCardsCreated = true;

            Debug.Log(
                "Starting cards created for Player " +
                (localPlayer + 1)
            );

            return;
        }

        // Player 1 drew
        if (game.Player1CardCount.Value > previousP1Count)
        {
            int cardsAdded =
                game.Player1CardCount.Value - previousP1Count;

            for (int i = 0; i < cardsAdded; i++)
            {
                CreateCard(
                    0,
                    game.Player1LastCard.Value,
                    false
                );
            }

            previousP1Count = game.Player1CardCount.Value;
        }

        // Player 2 drew
        if (game.Player2CardCount.Value > previousP2Count)
        {
            int cardsAdded =
                game.Player2CardCount.Value - previousP2Count;

            for (int i = 0; i < cardsAdded; i++)
            {
                CreateCard(
                    1,
                    game.Player2LastCard.Value,
                    false
                );
            }

            previousP2Count = game.Player2CardCount.Value;
        }
    }

    // =========================
    // STARTING CARDS
    // =========================

    private void CreateStartingCards(BoardGameManager game)
    {
        ClearAllCards();

        // First card is secret from opponent
        CreateCard(
            0,
            game.Player1Card1.Value,
            true
        );

        // Second card is public
        CreateCard(
            0,
            game.Player1Card2.Value,
            false
        );

        CreateCard(
            1,
            game.Player2Card1.Value,
            true
        );

        CreateCard(
            1,
            game.Player2Card2.Value,
            false
        );
    }

    // =========================
    // CREATE CARD
    // =========================

    private void CreateCard(
        int ownerPlayer,
        int cardValue,
        bool isSecretCard)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("Card Prefab is not assigned!");
            return;
        }

        if (BoardGameManager.Instance == null)
            return;

        int localPlayer =
            BoardGameManager.Instance.GetLocalPlayerNumber();

        if (localPlayer == -1)
            return;

        Transform spawnPoint =
            ownerPlayer == 0
                ? player1CardsPoint
                : player2CardsPoint;

        if (spawnPoint == null)
        {
            Debug.LogError(
                "Spawn Point missing for Player " +
                (ownerPlayer + 1)
            );

            return;
        }

        List<GameObject> cards =
            ownerPlayer == 0
                ? player1VisualCards
                : player2VisualCards;

        int cardIndex = cards.Count;

        Vector3 position =
            spawnPoint.position +
            spawnPoint.right * (cardIndex * cardSpacing);

        GameObject card = Instantiate(
            cardPrefab,
            position,
            spawnPoint.rotation
        );

        card.name =
            "Player" +
            (ownerPlayer + 1) +
            "_Card_" +
            (cardIndex + 1);

        // Preserve card clicking
        CardClick cardClick = card.GetComponent<CardClick>();

        if (cardClick != null)
        {
            cardClick.Setup(ownerPlayer);
        }
        else
        {
            Debug.LogWarning(
                "CardClick component missing from Card Prefab!"
            );
        }

        // Secret only to the opponent
        bool hideNumber =
            isSecretCard && ownerPlayer != localPlayer;

        TMP_Text numberText =
            card.GetComponentInChildren<TMP_Text>();

        if (numberText != null)
        {
            numberText.text =
                hideNumber ? "?" : cardValue.ToString();
        }
        else
        {
            Debug.LogWarning(
                "NumberText missing from Card Prefab!"
            );
        }

        cards.Add(card);

        Debug.Log(
            "Created card for Player " +
            (ownerPlayer + 1) +
            " | Card #" + cards.Count +
            " | Secret: " + isSecretCard +
            " | Hidden locally: " + hideNumber
        );
    }

    // =========================
    // CLEAR
    // =========================

    private void ClearAllCards()
    {
        foreach (GameObject card in player1VisualCards)
        {
            if (card != null)
                Destroy(card);
        }

        foreach (GameObject card in player2VisualCards)
        {
            if (card != null)
                Destroy(card);
        }

        player1VisualCards.Clear();
        player2VisualCards.Clear();
    }

    private void OnDestroy()
    {
        ClearAllCards();
    }
}
