using UnityEngine;

using Unity.Netcode;

using System.Collections;

using System.Collections.Generic;

public enum SkillType

{

    SkillDestroyer = 0,

    TargetBreaker = 1,

    DoubleDraw = 2,

    TurnBlock = 3,

    TotalBooster = 4,

    SecondChance = 5

}

public class BoardGameManager : NetworkBehaviour

{

    public static BoardGameManager Instance;

    [Header("Game Settings")]

    [SerializeField] private int totalRounds = 8;

    [SerializeField] private float roundResultDuration = 5f;

    [Header("Card Settings")]

    [SerializeField] private int minCardValue = 1;

    [SerializeField] private int maxCardValue = 10;

    // GAME STATE

    public NetworkVariable<int> CurrentRound = new(0);

    public NetworkVariable<int> TargetChooser = new(-1);

    public NetworkVariable<int> TargetNumber = new(0);

    // TRUE TOTALS - used for game rules

    public NetworkVariable<int> Player1Total = new(0);

    public NetworkVariable<int> Player2Total = new(0);

    // VISIBLE TOTALS - exclude the first secret card

    public NetworkVariable<int> Player1VisibleTotal = new(0);

    public NetworkVariable<int> Player2VisibleTotal = new(0);

    public NetworkVariable<int> CurrentTurn = new(-1);

    public NetworkVariable<bool> GameStarted = new(false);

    public NetworkVariable<bool> TargetSelected = new(false);

    // SCORES AND RESULTS

    public NetworkVariable<int> Player1Score = new(0);

    public NetworkVariable<int> Player2Score = new(0);

    // MANA CURRENCY - server authoritative, no maximum cap

    public NetworkVariable<int> Player1Mana = new(0);

    public NetworkVariable<int> Player2Mana = new(0);

    // SKILL SHOP PHASE 1 - controlled by the server

    public NetworkVariable<bool> ShopOpen = new(false);

    public NetworkVariable<float> ShopSecondsLeft = new(0f);

    [SerializeField] private float shopDuration = 10f;

    // -1 means no offer. Values 0..5 match SkillType.

    public NetworkVariable<int> ShopOffer1 = new(-1);

    public NetworkVariable<int> ShopOffer2 = new(-1);

    public NetworkVariable<int> ShopOffer3 = new(-1);

    // Purchased skills are retained until the skill-activation phase is implemented.

    public NetworkList<int> Player1OwnedSkills = new();

    public NetworkList<int> Player2OwnedSkills = new();

    private bool purchasedThisTurn;

    private const int RefreshManaCost = 2;

    private static readonly int[] SkillManaCosts = { 3, 3, 4, 6, 5, 3 };

    public static string GetSkillName(int skillId)

    {

        switch (skillId)

        {

            case 0: return "Skill Destroyer";

            case 1: return "Target Breaker";

            case 2: return "Double Draw";

            case 3: return "Turn Block";

            case 4: return "Total Booster";

            case 5: return "Second Chance";

            default: return "Unknown Skill";

        }

    }

    public static int GetSkillCost(int skillId)

    {

        return skillId >= 0 && skillId < SkillManaCosts.Length

            ? SkillManaCosts[skillId] : -1;

    }

    public int GetShopOffer(int slot)

    {

        switch (slot)

        {

            case 0: return ShopOffer1.Value;

            case 1: return ShopOffer2.Value;

            case 2: return ShopOffer3.Value;

            default: return -1;

        }

    }

    private void RollShopOffers()

    {

        if (!IsServer) return;

        // Shuffle 0..5; the first three are distinct.

        int[] ids = { 0, 1, 2, 3, 4, 5 };

        for (int i = ids.Length - 1; i > 0; i--)

        {

            int j = Random.Range(0, i + 1);

            int tmp = ids[i];

            ids[i] = ids[j];

            ids[j] = tmp;

        }

        ShopOffer1.Value = ids[0];

        ShopOffer2.Value = ids[1];

        ShopOffer3.Value = ids[2];

    }

    private bool IsValidShopSender(RpcParams rpcParams)

    {

        return IsServer && ShopOpen.Value && ShopSecondsLeft.Value > 0f &&

               GameStarted.Value && TargetSelected.Value &&

               !RoundEnded.Value && !GameFinished.Value &&

               CurrentTurn.Value >= 0 &&

               GetPlayerNumberFromClientId(rpcParams.Receive.SenderClientId) == CurrentTurn.Value &&

               BlockedPlayer.Value != CurrentTurn.Value;

    }

    [Rpc(SendTo.Server)]

    public void RefreshShopRpc(RpcParams rpcParams = default)

    {

        if (!IsValidShopSender(rpcParams) || purchasedThisTurn) return;

        NetworkVariable<int> mana = CurrentTurn.Value == 0 ? Player1Mana : Player2Mana;

        if (mana.Value < RefreshManaCost) return;

        mana.Value -= RefreshManaCost;

        RollShopOffers();

        // Intentionally do not reset the countdown.

    }

    [Rpc(SendTo.Server)]

    public void BuyShopSkillRpc(int slot, RpcParams rpcParams = default)

    {

        if (!IsValidShopSender(rpcParams) || purchasedThisTurn) return;

        int skillId = GetShopOffer(slot);

        int cost = GetSkillCost(skillId);

        if (cost < 0) return;

        NetworkVariable<int> mana = CurrentTurn.Value == 0 ? Player1Mana : Player2Mana;

        if (mana.Value < cost) return;

        mana.Value -= cost;

        purchasedThisTurn = true;

        if (CurrentTurn.Value == 0)

            Player1OwnedSkills.Add(skillId);

        else

            Player2OwnedSkills.Add(skillId);

        Debug.Log("Player " + (CurrentTurn.Value + 1) +

                  " bought " + GetSkillName(skillId) + " for " + cost + " Mana");

        CloseShop();

    }

    public NetworkVariable<int> RoundWinner = new(-1);

    public NetworkVariable<int> FinalWinner = new(-1);

    public NetworkVariable<bool> RoundEnded = new(false);

    public NetworkVariable<bool> GameFinished = new(false);

    // PLAYER IDS

    public NetworkVariable<ulong> Player1ClientId =

        new(ulong.MaxValue);

    public NetworkVariable<ulong> Player2ClientId =

        new(ulong.MaxValue);

    // CARD NETWORK DATA

    public NetworkVariable<int> Player1CardCount = new(0);

    public NetworkVariable<int> Player2CardCount = new(0);

    public NetworkVariable<int> Player1LastCard = new(0);

    public NetworkVariable<int> Player2LastCard = new(0);

    public NetworkVariable<int> Player1Card1 = new(0);

    public NetworkVariable<int> Player1Card2 = new(0);

    public NetworkVariable<int> Player2Card1 = new(0);

    public NetworkVariable<int> Player2Card2 = new(0);

    private readonly List<NumberCard> deck = new();

    private readonly List<NumberCard> player1Cards = new();

    private readonly List<NumberCard> player2Cards = new();

    // Coin flip: 0 = Heads, 1 = Tails, -1 = not chosen / not tossed.
    public NetworkVariable<int> Player1CoinSide = new(-1);
    public NetworkVariable<int> CoinResult = new(-1);
    public NetworkVariable<int> Player1ProposedTarget = new(0);
    public NetworkVariable<int> Player2ProposedTarget = new(0);
    public NetworkVariable<bool> Player1Ready = new(false);
    public NetworkVariable<bool> Player2Ready = new(false);

    private int consecutivePasses = 0;

    // Server-authoritative block: -1 = nobody, 0 = P1, 1 = P2.

    public NetworkVariable<int> BlockedPlayer = new(-1);

    // Server-authoritative protection for one future busting draw.
    public NetworkVariable<bool> Player1SecondChance = new(false);
    public NetworkVariable<bool> Player2SecondChance = new(false);

    public bool IsLocalPlayerBlocked()

    {

        int player = GetLocalPlayerNumber();

        return player >= 0 && BlockedPlayer.Value == player;

    }

    private Coroutine nextRoundCoroutine;

    private void Update()

    {

        if (!IsServer || !ShopOpen.Value)

            return;

        ShopSecondsLeft.Value = Mathf.Max(0f, ShopSecondsLeft.Value - Time.deltaTime);

        if (ShopSecondsLeft.Value <= 0f)

            CloseShop();

    }

    private void OpenShop()

    {

        if (!IsServer || !GameStarted.Value || !TargetSelected.Value ||

            RoundEnded.Value || GameFinished.Value || CurrentTurn.Value < 0)

            return;

        purchasedThisTurn = false;

        RollShopOffers();

        ShopSecondsLeft.Value = Mathf.Max(0.1f, shopDuration);

        ShopOpen.Value = true;

        Debug.Log("Skill shop opened for player " + (CurrentTurn.Value + 1));

    }

    private void CloseShop()

    {

        if (!IsServer)

            return;

        ShopOpen.Value = false;

        ShopSecondsLeft.Value = 0f;

    }

    [Rpc(SendTo.Server)]

    public void SkipShopRpc(RpcParams rpcParams = default)

    {

        if (!IsServer || !ShopOpen.Value || !GameStarted.Value ||

            RoundEnded.Value || GameFinished.Value)

            return;

        int player = GetPlayerNumberFromClientId(rpcParams.Receive.SenderClientId);

        if (player != CurrentTurn.Value || BlockedPlayer.Value == player)

            return;

        CloseShop();

    }

    private void Awake()

    {

        Instance = this;

    }

    public override void OnNetworkSpawn()

    {

        Debug.Log("BoardGameManager spawned.");

        if (!IsServer)

            return;

        NetworkManager.OnClientConnectedCallback +=

            OnClientConnected;

        NetworkManager.OnClientDisconnectCallback +=

            OnClientDisconnected;

        RegisterPlayers();

        CheckPlayers();

    }

    // CONNECTION

    private void OnClientConnected(ulong clientId)

    {

        if (!IsServer)

            return;

        Debug.Log("Player Connected: " + clientId);

        RegisterPlayers();

        CheckPlayers();

    }

    private void OnClientDisconnected(ulong clientId)

    {

        if (!IsServer)

            return;

        Debug.Log("Player Disconnected: " + clientId);

        GameStarted.Value = false;

        BlockedPlayer.Value = -1;
        Player1SecondChance.Value = false;
        Player2SecondChance.Value = false;

            CloseShop();

        CurrentTurn.Value = -1;

        if (nextRoundCoroutine != null)

        {

            StopCoroutine(nextRoundCoroutine);

            nextRoundCoroutine = null;

        }

        if (Player1ClientId.Value == clientId)

            Player1ClientId.Value = ulong.MaxValue;

        if (Player2ClientId.Value == clientId)

            Player2ClientId.Value = ulong.MaxValue;

    }

    private void RegisterPlayers()

    {

        if (!IsServer)

            return;

        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)

        {

            if (Player1ClientId.Value == clientId ||

                Player2ClientId.Value == clientId)

                continue;

            if (Player1ClientId.Value == ulong.MaxValue)

            {

                Player1ClientId.Value = clientId;

                Debug.Log("Player 1 Client ID: " + clientId);

                continue;

            }

            if (Player2ClientId.Value == ulong.MaxValue)

            {

                Player2ClientId.Value = clientId;

                Debug.Log("Player 2 Client ID: " + clientId);

            }

        }

    }

    private void CheckPlayers()

    {

        if (!IsServer)

            return;

        int playerCount = NetworkManager.ConnectedClients.Count;

        Debug.Log("Players connected: " + playerCount + "/2");

        if (playerCount >= 2 &&

            Player1ClientId.Value != ulong.MaxValue &&

            Player2ClientId.Value != ulong.MaxValue &&

            !GameStarted.Value)

        {

            StartGame();

        }

    }

    // GAME START

    private void StartGame()

    {

        if (!IsServer)

            return;

        Debug.Log("========== GAME STARTED ==========");

        if (nextRoundCoroutine != null)

        {

            StopCoroutine(nextRoundCoroutine);

            nextRoundCoroutine = null;

        }

        GameStarted.Value = true;

            CloseShop();

        GameFinished.Value = false;

        RoundEnded.Value = false;

        CurrentRound.Value = 1;

        Player1Score.Value = 0;

        Player2Score.Value = 0;

        // Reset Mana only when a NEW MATCH begins

        Player1Mana.Value = 2;

        Player2Mana.Value = 2;

        Player1SecondChance.Value = false;
        Player2SecondChance.Value = false;

        Player1OwnedSkills.Clear();

        Player2OwnedSkills.Clear();

        purchasedThisTurn = false;

        BlockedPlayer.Value = -1;

        RoundWinner.Value = -1;

        FinalWinner.Value = -1;



        CreateDeck();

        StartRound();

    }

    // DECK

    private void CreateDeck()

    {

        deck.Clear();

        for (int value = minCardValue;

             value <= maxCardValue;

             value++)

        {

            for (int copy = 0; copy < 4; copy++)

            {

                deck.Add(new NumberCard(value));

            }

        }

        ShuffleDeck();

        Debug.Log("Deck created. Cards: " + deck.Count);

    }

    private void ShuffleDeck()

    {

        for (int i = deck.Count - 1; i > 0; i--)

        {

            int randomIndex = Random.Range(0, i + 1);

            NumberCard temp = deck[i];

            deck[i] = deck[randomIndex];

            deck[randomIndex] = temp;

        }

    }

    private NumberCard TakeCardFromDeck()

    {

        if (deck.Count == 0)

        {

            Debug.Log("Deck empty. Creating new deck.");

            CreateDeck();

        }

        NumberCard card = deck[0];

        deck.RemoveAt(0);

        return card;

    }

    // ROUND START

    private void StartRound()

    {

        if (!IsServer || GameFinished.Value)

            return;

        Debug.Log(

            "========== ROUND " +

            CurrentRound.Value +

            " =========="

        );

        CloseShop();

        RoundEnded.Value = false;

        Player1SecondChance.Value = false;
        Player2SecondChance.Value = false;

        BlockedPlayer.Value = -1;

        RoundWinner.Value = -1;

        TargetNumber.Value = 0;

        TargetSelected.Value = false;

        Player1Total.Value = 0;

        Player2Total.Value = 0;

        // Reset public totals for the new round

        Player1VisibleTotal.Value = 0;

        Player2VisibleTotal.Value = 0;

        Player1CardCount.Value = 0;

        Player2CardCount.Value = 0;

        Player1LastCard.Value = 0;

        Player2LastCard.Value = 0;

        Player1Card1.Value = 0;

        Player1Card2.Value = 0;

        Player2Card1.Value = 0;

        Player2Card2.Value = 0;

        CurrentTurn.Value = -1;

        player1Cards.Clear();

        player2Cards.Clear();

        consecutivePasses = 0;

        RunTargetLottery();

    }

    // COIN FLIP: player 1 chooses a side; player 2 automatically gets the opposite.
    private void RunTargetLottery()
    {
        if (!IsServer) return;
        TargetChooser.Value = -1;
        Player1CoinSide.Value = -1;
        CoinResult.Value = -1;
        Player1ProposedTarget.Value = 0;
        Player2ProposedTarget.Value = 0;
        Player1Ready.Value = false;
        Player2Ready.Value = false;
    }

    [Rpc(SendTo.Server)]
    public void ChooseCoinSideRpc(int side, RpcParams rpcParams = default)
    {
        if (!IsServer || !GameStarted.Value || GameFinished.Value ||
            RoundEnded.Value || TargetSelected.Value || CoinResult.Value != -1)
            return;
        if (rpcParams.Receive.SenderClientId != Player1ClientId.Value) return;
        if (side != 0 && side != 1) return;
        if (Player1CoinSide.Value != -1) return; // Lock the choice once made.

        Player1CoinSide.Value = side;
        TryResolveCoinFlip();
    }

    // Both players submit their own number; the server checks the sender and range.
    [Rpc(SendTo.Server)]
    public void SubmitTargetRpc(int target, RpcParams rpcParams = default)
    {
        if (!IsServer || !GameStarted.Value || GameFinished.Value ||
            RoundEnded.Value || TargetSelected.Value || CoinResult.Value != -1)
            return;
        if (target < 35 || target > 50)
        {
            Debug.LogWarning("Target number must be between 35 and 50.");
            return;
        }

        ulong sender = rpcParams.Receive.SenderClientId;
        if (sender == Player1ClientId.Value)
        {
            if (Player1Ready.Value) return;
            Player1ProposedTarget.Value = target;
            Player1Ready.Value = true;
        }
        else if (sender == Player2ClientId.Value)
        {
            if (Player2Ready.Value) return;
            Player2ProposedTarget.Value = target;
            Player2Ready.Value = true;
        }
        else return;

        TryResolveCoinFlip();
    }

    private void TryResolveCoinFlip()
    {
        if (!IsServer || !GameStarted.Value || TargetSelected.Value ||
            Player1CoinSide.Value == -1 || !Player1Ready.Value || !Player2Ready.Value)
            return;

        int result = Random.Range(0, 2);
        int winner = result == Player1CoinSide.Value ? 0 : 1;
        CoinResult.Value = result;
        TargetChooser.Value = winner;
        TargetNumber.Value = winner == 0
            ? Player1ProposedTarget.Value : Player2ProposedTarget.Value;
        TargetSelected.Value = true;
        DealStartingCards();
        CurrentTurn.Value = winner; // Coin winner plays first.
        // The first turn of the match uses the initial 2 Mana.
        // In later rounds, this is a new turn and earns +2 Mana.
        if (CurrentRound.Value > 1)
            GrantTurnMana(winner);
        consecutivePasses = 0;
        OpenShop();
        Debug.Log("COIN FLIP: " + (result == 0 ? "Heads" : "Tails") +
                  " | Winner: Player " + (winner + 1) +
                  " | Target: " + TargetNumber.Value);
    }

    // STARTING CARDS

    private void DealStartingCards()

    {

        if (!IsServer)

            return;

        player1Cards.Clear();

        player2Cards.Clear();

        Player1Total.Value = 0;

        Player2Total.Value = 0;

        Player1VisibleTotal.Value = 0;

        Player2VisibleTotal.Value = 0;

        Player1CardCount.Value = 0;

        Player2CardCount.Value = 0;

        Player1LastCard.Value = 0;

        Player2LastCard.Value = 0;

        Player1Card1.Value = 0;

        Player1Card2.Value = 0;

        Player2Card1.Value = 0;

        Player2Card2.Value = 0;

        // PLAYER 1 - FIRST SECRET CARD

        NumberCard p1Card1 = TakeCardFromDeck();

        player1Cards.Add(p1Card1);

        Player1Card1.Value = p1Card1.Value;

        Player1Total.Value += p1Card1.Value;

        // Secret card is NOT added to visible total

        // PLAYER 1 - SECOND PUBLIC CARD

        NumberCard p1Card2 = TakeCardFromDeck();

        player1Cards.Add(p1Card2);

        Player1Card2.Value = p1Card2.Value;

        Player1Total.Value += p1Card2.Value;

        Player1VisibleTotal.Value += p1Card2.Value;

        Player1CardCount.Value = 2;

        Player1LastCard.Value = p1Card2.Value;

        // PLAYER 2 - FIRST SECRET CARD

        NumberCard p2Card1 = TakeCardFromDeck();

        player2Cards.Add(p2Card1);

        Player2Card1.Value = p2Card1.Value;

        Player2Total.Value += p2Card1.Value;

        // Secret card is NOT added to visible total

        // PLAYER 2 - SECOND PUBLIC CARD

        NumberCard p2Card2 = TakeCardFromDeck();

        player2Cards.Add(p2Card2);

        Player2Card2.Value = p2Card2.Value;

        Player2Total.Value += p2Card2.Value;

        Player2VisibleTotal.Value += p2Card2.Value;

        Player2CardCount.Value = 2;

        Player2LastCard.Value = p2Card2.Value;

        Debug.Log(

            "P1 Starting Cards: " +

            p1Card1.Value + ", " + p1Card2.Value

        );

        Debug.Log(

            "P2 Starting Cards: " +

            p2Card1.Value + ", " + p2Card2.Value

        );

    }

    // SECOND CHANCE: activate during your turn, after the shop closes.
    [Rpc(SendTo.Server)]
    public void UseSecondChanceRpc(int skillIndex, RpcParams rpcParams = default)
    {
        if (!CanPerformAction()) return;

        int player = GetPlayerNumberFromClientId(rpcParams.Receive.SenderClientId);
        if (player < 0 || player != CurrentTurn.Value) return;

        NetworkList<int> owned = player == 0 ? Player1OwnedSkills : Player2OwnedSkills;
        if (skillIndex < 0 || skillIndex >= owned.Count ||
            owned[skillIndex] != (int)SkillType.SecondChance) return;

        NetworkVariable<bool> protection = player == 0
            ? Player1SecondChance : Player2SecondChance;
        if (protection.Value) return;

        owned.RemoveAt(skillIndex);
        protection.Value = true;
        Debug.Log("SECOND CHANCE ARMED | Player " + (player + 1));
    }

    // Returns true only when Second Chance prevented a bust.
    // Snapshots include passive side effects (target and mana) and public card state.
    private bool GiveProtectedCardToPlayer(int player)
    {
        if (!IsServer) return false;

        List<NumberCard> cards = player == 0 ? player1Cards : player2Cards;
        int countBefore = cards.Count;
        int p1Total = Player1Total.Value, p2Total = Player2Total.Value;
        int p1Visible = Player1VisibleTotal.Value, p2Visible = Player2VisibleTotal.Value;
        int p1Count = Player1CardCount.Value, p2Count = Player2CardCount.Value;
        int p1Last = Player1LastCard.Value, p2Last = Player2LastCard.Value;
        int target = TargetNumber.Value;
        int p1Mana = Player1Mana.Value, p2Mana = Player2Mana.Value;

        GiveCardToPlayer(player);
        if (GetPlayerTotal(player) <= TargetNumber.Value) return false;

        NetworkVariable<bool> protection = player == 0
            ? Player1SecondChance : Player2SecondChance;
        if (!protection.Value) return false;

        if (cards.Count > countBefore)
        {
            NumberCard canceledCard = cards[cards.Count - 1];
            cards.RemoveAt(cards.Count - 1);
            // Undo the deck removal as well; keep the deck's original order.
            deck.Insert(0, canceledCard);
        }

        Player1Total.Value = p1Total;
        Player2Total.Value = p2Total;
        Player1VisibleTotal.Value = p1Visible;
        Player2VisibleTotal.Value = p2Visible;
        Player1CardCount.Value = p1Count;
        Player2CardCount.Value = p2Count;
        Player1LastCard.Value = p1Last;
        Player2LastCard.Value = p2Last;
        TargetNumber.Value = target;
        Player1Mana.Value = p1Mana;
        Player2Mana.Value = p2Mana;
        protection.Value = false;

        Debug.Log("SECOND CHANCE SAVED PLAYER " + (player + 1));
        return true;
    }

    // DRAW

    [Rpc(SendTo.Server)]

    public void DrawCardRpc(RpcParams rpcParams = default)

    {

        if (!CanPerformAction())

            return;

        ulong senderClientId =

            rpcParams.Receive.SenderClientId;

        int playerNumber =

            GetPlayerNumberFromClientId(senderClientId);

        if (playerNumber == -1 ||

            CurrentTurn.Value != playerNumber ||

             BlockedPlayer.Value == playerNumber)

        {

            Debug.LogWarning("Invalid Draw request.");

            return;

        }

        consecutivePasses = 0;

        if (GiveProtectedCardToPlayer(playerNumber))
        {
            SwitchTurn();
            return;
        }

        int total = GetPlayerTotal(playerNumber);

        Debug.Log(

            "Player " + (playerNumber + 1) +

            " drew a card. Total: " + total

        );

        // Exceeding target causes immediate loss

        if (total > TargetNumber.Value)

        {

            Debug.Log(

                "PLAYER " + (playerNumber + 1) +

                " EXCEEDED TARGET!"

            );

            int winner = playerNumber == 0 ? 1 : 0;

            EndRound(winner);

            return;

        }

        // A 2+2 passive can also put the opponent over the new target.

        int opponent = playerNumber == 0 ? 1 : 0;

        if (GetPlayerTotal(opponent) > TargetNumber.Value)

        {

            EndRound(playerNumber);

            return;

        }

        SwitchTurn();

    }

    // PASS

    [Rpc(SendTo.Server)]

    public void PassTurnRpc(RpcParams rpcParams = default)

    {

        if (!CanPerformAction())

            return;

        ulong senderClientId =

            rpcParams.Receive.SenderClientId;

        int playerNumber =

            GetPlayerNumberFromClientId(senderClientId);

        if (playerNumber == -1 ||

            CurrentTurn.Value != playerNumber ||

             BlockedPlayer.Value == playerNumber)

        {

            Debug.LogWarning("Invalid Pass request.");

            return;

        }

        consecutivePasses++;

        Debug.Log(

            "Player " + (playerNumber + 1) + " PASSED."

        );

        Debug.Log(

            "Consecutive Passes: " + consecutivePasses

        );

        if (consecutivePasses >= 2)

        {

            Debug.Log("BOTH PLAYERS PASSED");

            ResolveRound();

            return;

        }

        SwitchTurn();

    }

    private bool CanPerformAction()

    {

        return IsServer &&

               GameStarted.Value &&

               TargetSelected.Value &&

               !RoundEnded.Value &&

               !GameFinished.Value &&

               CurrentTurn.Value >= 0 &&

               !ShopOpen.Value &&

               BlockedPlayer.Value != CurrentTurn.Value;

    }

    // GIVE CARD - server-authoritative, including rare consecutive-card passives.

    // Passives only trigger on an actual draw, not when starting cards are dealt.

    private void GiveCardToPlayer(int playerNumber)

    {

        if (!IsServer)

            return;

        List<NumberCard> cards = playerNumber == 0 ? player1Cards : player2Cards;

        int previousValue = cards.Count > 0 ? cards[cards.Count - 1].Value : -1;

        NumberCard card = TakeCardFromDeck();

        cards.Add(card);

        if (playerNumber == 0)

        {

            Player1Total.Value += card.Value;

            Player1VisibleTotal.Value += card.Value;

            Player1CardCount.Value = cards.Count;

            Player1LastCard.Value = card.Value;

        }

        else

        {

            Player2Total.Value += card.Value;

            Player2VisibleTotal.Value += card.Value;

            Player2CardCount.Value = cards.Count;

            Player2LastCard.Value = card.Value;

        }

        Debug.Log("P" + (playerNumber + 1) + " received card: " + card.Value);

        // The matching cards must be consecutive in this player's own draw history.

        if (previousValue != card.Value)

            return;

        switch (card.Value)

        {

            case 1:

                // The bonus is public and affects the true total as well.

                if (playerNumber == 0)

                {

                    Player1Total.Value += 3;

                    Player1VisibleTotal.Value += 3;

                }

                else

                {

                    Player2Total.Value += 3;

                    Player2VisibleTotal.Value += 3;

                }

                Debug.Log("PASSIVE 1+1 | Player " + (playerNumber + 1) + " gained +3 total");

                break;

            case 2:

                // Keep the target positive, like other target-changing mechanics.

                TargetNumber.Value = Mathf.Max(1, TargetNumber.Value - 2);

                Debug.Log("PASSIVE 2+2 | Target decreased to " + TargetNumber.Value);

                break;

            case 3:

                if (playerNumber == 0)

                    Player1Mana.Value = Mathf.Min(20, Player1Mana.Value + 4);

                else

                    Player2Mana.Value = Mathf.Min(20, Player2Mana.Value + 4);

                Debug.Log("PASSIVE 3+3 | Player " + (playerNumber + 1) + " gained +4 Mana");

                break;

        }

    }

    // TURN

    private void SwitchTurn()

    {

        if (!IsServer || RoundEnded.Value)

            return;

        int nextPlayer = CurrentTurn.Value == 0 ? 1 : 0;

        if (CurrentTurn.Value < 0)

            return;

        if (BlockedPlayer.Value == nextPlayer)

        {

            // Skip exactly one upcoming turn and consume the block.

            BlockedPlayer.Value = -1;

            consecutivePasses = 0;

            CloseShop();

            Debug.Log("TURN BLOCK | Player " + (nextPlayer + 1) +

                      " skipped their turn");

            // CurrentTurn stays with the player who played.
            // Skipping the opponent creates a new active turn for this player.
            GrantTurnMana(CurrentTurn.Value);

            OpenShop();

            return;

        }

        CurrentTurn.Value = nextPlayer;

        GrantTurnMana(nextPlayer);

        OpenShop();

        Debug.Log("Turn switched to Player " + (CurrentTurn.Value + 1));

    }

    // Mana is awarded by the server only when a real turn begins.
    // The first turn of the match is excluded (players already start with 2).
    private void GrantTurnMana(int playerNumber)
    {
        if (!IsServer || !GameStarted.Value || RoundEnded.Value || GameFinished.Value)
            return;

        if (playerNumber == 0)
            Player1Mana.Value = Mathf.Min(20, Player1Mana.Value + 2);
        else if (playerNumber == 1)
            Player2Mana.Value = Mathf.Min(20, Player2Mana.Value + 2);
    }

    // ROUND RESULT

    private void ResolveRound()

    {

        if (!IsServer || RoundEnded.Value)

            return;

        int target = TargetNumber.Value;

        // IMPORTANT: Always compare REAL totals

        int p1 = Player1Total.Value;

        int p2 = Player2Total.Value;

        bool p1Exceeded = p1 > target;

        bool p2Exceeded = p2 > target;

        int winner;

        if (p1Exceeded && !p2Exceeded)

        {

            winner = 1;

        }

        else if (p2Exceeded && !p1Exceeded)

        {

            winner = 0;

        }

        else if (p1Exceeded && p2Exceeded)

        {

            winner = 2;

        }

        else

        {

            int p1Difference = target - p1;

            int p2Difference = target - p2;

            if (p1Difference < p2Difference)

                winner = 0;

            else if (p2Difference < p1Difference)

                winner = 1;

            else

                winner = 2;

        }

        EndRound(winner);

    }

    // winner: 0 = Player 1

    // winner: 1 = Player 2

    // winner: 2 = Tie

    private void EndRound(int winner)

    {

        if (!IsServer ||

            RoundEnded.Value ||

            GameFinished.Value)

            return;

        CloseShop();

        RoundEnded.Value = true;

        Player1SecondChance.Value = false;
        Player2SecondChance.Value = false;

        BlockedPlayer.Value = -1;

        CurrentTurn.Value = -1;

        RoundWinner.Value = winner;

        if (winner == 0)

        {

            Player1Score.Value += 10;

            Debug.Log("ROUND WINNER: PLAYER 1 (+10)");

        }

        else if (winner == 1)

        {

            Player2Score.Value += 10;

            Debug.Log("ROUND WINNER: PLAYER 2 (+10)");

        }

        else

        {

            Player1Score.Value += 5;

            Player2Score.Value += 5;

            Debug.Log("ROUND TIE (+5 EACH)");

        }

        Debug.Log(

            "SCORE: P1 = " + Player1Score.Value +

            " | P2 = " + Player2Score.Value

        );

        if (nextRoundCoroutine != null)

            StopCoroutine(nextRoundCoroutine);

        nextRoundCoroutine = StartCoroutine(

            AdvanceAfterResult()

        );

    }

    private IEnumerator AdvanceAfterResult()

    {

        yield return new WaitForSeconds(

            Mathf.Max(0f, roundResultDuration)

        );

        nextRoundCoroutine = null;

        if (!IsServer || !GameStarted.Value)

            yield break;

        if (CurrentRound.Value >= totalRounds)

        {

            FinishGame();

        }

        else

        {

            CurrentRound.Value++;

            StartRound();

        }

    }

    // FINAL RESULT

    private void FinishGame()

    {

        if (!IsServer || GameFinished.Value)

            return;

        CloseShop();

        GameFinished.Value = true;

        RoundEnded.Value = true;

        CurrentTurn.Value = -1;

        if (Player1Score.Value > Player2Score.Value)

            FinalWinner.Value = 0;

        else if (Player2Score.Value > Player1Score.Value)

            FinalWinner.Value = 1;

        else

            FinalWinner.Value = 2;

        Debug.Log("========== GAME FINISHED ==========");

        if (FinalWinner.Value == 2)

        {

            Debug.Log("FINAL RESULT: DRAW");

        }

        else

        {

            Debug.Log(

                "FINAL WINNER: PLAYER " +

                (FinalWinner.Value + 1)

            );

        }

        Debug.Log(

            "FINAL SCORE: P1 = " + Player1Score.Value +

            " | P2 = " + Player2Score.Value

        );

    }

    // PLAYER HELPERS

    private int GetPlayerNumberFromClientId(ulong clientId)

    {

        if (Player1ClientId.Value == clientId)

            return 0;

        if (Player2ClientId.Value == clientId)

            return 1;

        return -1;

    }

    private int GetPlayerTotal(int playerNumber)

    {

        return playerNumber == 0

            ? Player1Total.Value

            : Player2Total.Value;

    }

    public int GetLocalPlayerNumber()

    {

        if (NetworkManager.Singleton == null ||

            !NetworkManager.Singleton.IsClient)

            return -1;

        ulong localId =

            NetworkManager.Singleton.LocalClientId;

        if (Player1ClientId.Value == localId)

            return 0;

        if (Player2ClientId.Value == localId)

            return 1;

        return -1;

    }

    public bool AmITheTargetChooser()

    {

        int localPlayer = GetLocalPlayerNumber();

        return localPlayer != -1 &&

               localPlayer == TargetChooser.Value;

    }

    public bool IsMyTurn()

    {

        int localPlayer = GetLocalPlayerNumber();

        return localPlayer != -1 &&

               !RoundEnded.Value &&

               !GameFinished.Value &&

               localPlayer == CurrentTurn.Value;

    }

    // CLEANUP

    // Skill Destroyer: server validates both cards before consuming the skill.

    [Rpc(SendTo.Server)]

    public void DestroyOpponentSkillRpc(

        int destroyerIndex,

        int targetSkillIndex,

        RpcParams rpcParams = default)

    {

        if (!CanPerformAction())

            return;

        int player = GetPlayerNumberFromClientId(

            rpcParams.Receive.SenderClientId);

        if (player < 0 || player != CurrentTurn.Value ||

            BlockedPlayer.Value == player)

            return;

        NetworkList<int> mySkills = player == 0

            ? Player1OwnedSkills

            : Player2OwnedSkills;

        NetworkList<int> opponentSkills = player == 0

            ? Player2OwnedSkills

            : Player1OwnedSkills;

        if (destroyerIndex < 0 ||

            destroyerIndex >= mySkills.Count ||

            mySkills[destroyerIndex] != (int)SkillType.SkillDestroyer)

            return;

        if (targetSkillIndex < 0 ||

            targetSkillIndex >= opponentSkills.Count)

            return;

        int destroyedSkill = opponentSkills[targetSkillIndex];

        opponentSkills.RemoveAt(targetSkillIndex);

        mySkills.RemoveAt(destroyerIndex);

        Debug.Log("SKILL DESTROYER USED | Player: " +

            (player + 1) + " | Destroyed: " +

            GetSkillName(destroyedSkill));

    }

    // Target Breaker: usable by either player at any time during an active round.

    [Rpc(SendTo.Server)]

    public void UseTargetBreakerRpc(int skillIndex, RpcParams rpcParams = default)

    {

        if (!IsServer || !GameStarted.Value || !TargetSelected.Value ||

            RoundEnded.Value || GameFinished.Value)

            return;

        int player = GetPlayerNumberFromClientId(rpcParams.Receive.SenderClientId);

        if (player < 0 || BlockedPlayer.Value == player)

            return;

        NetworkList<int> mySkills = player == 0

            ? Player1OwnedSkills : Player2OwnedSkills;

        if (skillIndex < 0 || skillIndex >= mySkills.Count ||

            mySkills[skillIndex] != (int)SkillType.TargetBreaker)

            return;

        if (TargetNumber.Value <= 3)

            return;

        TargetNumber.Value -= 3;

        mySkills.RemoveAt(skillIndex);

        Debug.Log("TARGET BREAKER USED | Player: " + (player + 1) +

                  " | New Target: " + TargetNumber.Value);

    }

    // Double Draw: one additional card, usable outside the player's turn.

    [Rpc(SendTo.Server)]

    public void UseDoubleDrawRpc(int skillIndex, RpcParams rpcParams = default)

    {

        if (!IsServer || !GameStarted.Value || !TargetSelected.Value ||

            RoundEnded.Value || GameFinished.Value)

            return;

        int player = GetPlayerNumberFromClientId(rpcParams.Receive.SenderClientId);

        if (player < 0 || BlockedPlayer.Value == player)

            return;

        NetworkList<int> mySkills = player == 0

            ? Player1OwnedSkills : Player2OwnedSkills;

        if (skillIndex < 0 || skillIndex >= mySkills.Count ||

            mySkills[skillIndex] != (int)SkillType.DoubleDraw)

            return;

        mySkills.RemoveAt(skillIndex);

        if (GiveProtectedCardToPlayer(player))
            return;

        int total = GetPlayerTotal(player);

        Debug.Log("DOUBLE DRAW USED | Player: " + (player + 1) +

                  " | New Total: " + total);

        if (total > TargetNumber.Value)

        {

            int winner = player == 0 ? 1 : 0;

            EndRound(winner);

            return;

        }

        // Handle an opponent exceeding the target after a 2+2 passive.

        int opponent = player == 0 ? 1 : 0;

        if (GetPlayerTotal(opponent) > TargetNumber.Value)

            EndRound(player);

    }

    // Turn Block: blocks the opponent's next turn and their skills until skipped.

    [Rpc(SendTo.Server)]

    public void UseTurnBlockRpc(int skillIndex, RpcParams rpcParams = default)

    {

        if (!IsServer || !GameStarted.Value || !TargetSelected.Value ||

            RoundEnded.Value || GameFinished.Value)

            return;

        int player = GetPlayerNumberFromClientId(rpcParams.Receive.SenderClientId);

        if (player < 0 || BlockedPlayer.Value == player)

            return;

        NetworkList<int> skills = player == 0 ? Player1OwnedSkills : Player2OwnedSkills;

        if (skillIndex < 0 || skillIndex >= skills.Count ||

            skills[skillIndex] != (int)SkillType.TurnBlock)

            return;

        int opponent = player == 0 ? 1 : 0;

        if (BlockedPlayer.Value == opponent)

            return; // Already blocked; don't consume a second card.

        skills.RemoveAt(skillIndex);

        BlockedPlayer.Value = opponent;

        Debug.Log("TURN BLOCK USED | Player " + (player + 1) +

                  " blocked Player " + (opponent + 1));

        // If the opponent is currently taking their turn, skip it now.

        if (CurrentTurn.Value == opponent)

        {

            BlockedPlayer.Value = -1;

            consecutivePasses = 0;

            CloseShop();

            CurrentTurn.Value = player;
            GrantTurnMana(player);

            OpenShop();

            Debug.Log("TURN BLOCK | Player " + (opponent + 1) +

                      " skipped their current turn");

        }

    }

    // Total Booster: adds 1-5 to the opponent's total without drawing a card.

    // Can be used outside your turn, unless Turn Block is active on you.

    [Rpc(SendTo.Server)]

    public void UseTotalBoosterRpc(int skillIndex, RpcParams rpcParams = default)

    {

        if (!IsServer || !GameStarted.Value || !TargetSelected.Value ||

            RoundEnded.Value || GameFinished.Value)

            return;

        int player = GetPlayerNumberFromClientId(rpcParams.Receive.SenderClientId);

        if (player < 0 || BlockedPlayer.Value == player)

            return;

        NetworkList<int> skills = player == 0

            ? Player1OwnedSkills : Player2OwnedSkills;

        if (skillIndex < 0 || skillIndex >= skills.Count ||

            skills[skillIndex] != (int)SkillType.TotalBooster)

            return;

        int opponent = player == 0 ? 1 : 0;

        int boost = Random.Range(1, 6); // Inclusive 1..5 for integers.

        skills.RemoveAt(skillIndex);

        if (opponent == 0)

        {

            Player1Total.Value += boost;

            Player1VisibleTotal.Value += boost;

        }

        else

        {

            Player2Total.Value += boost;

            Player2VisibleTotal.Value += boost;

        }

        int opponentTotal = GetPlayerTotal(opponent);

        Debug.Log("TOTAL BOOSTER USED | Player " + (player + 1) +

                  " | Opponent: " + (opponent + 1) +

                  " | Boost: +" + boost +

                  " | Opponent total: " + opponentTotal);

        if (opponentTotal > TargetNumber.Value)

            EndRound(player);

    }

    public override void OnNetworkDespawn()

    {

        if (IsServer && NetworkManager != null)

        {

            NetworkManager.OnClientConnectedCallback -=

                OnClientConnected;

            NetworkManager.OnClientDisconnectCallback -=

                OnClientDisconnected;

        }

        if (nextRoundCoroutine != null)

        {

            StopCoroutine(nextRoundCoroutine);

            nextRoundCoroutine = null;

        }

        base.OnNetworkDespawn();

    }

    public override void OnDestroy()

    {

        if (Instance == this)

            Instance = null;

        base.OnDestroy();

    }

}
