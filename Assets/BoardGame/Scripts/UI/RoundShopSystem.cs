
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class RoundShopSystem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private Button skipButton;

    [Header("Skill Cards")]
    [SerializeField] private TMP_Text[] skillNames = new TMP_Text[3];
    [SerializeField] private TMP_Text[] skillPrices = new TMP_Text[3];
    [SerializeField] private Button[] buyButtons = new Button[3];
    [SerializeField] private Button refreshButton;

    private BoardGameManager manager;
    private UnityAction[] buyActions;

    private void Awake()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (skipButton != null)
            skipButton.onClick.AddListener(OnSkipClicked);

        buyActions = new UnityAction[buyButtons.Length];

        for (int i = 0; i < buyButtons.Length; i++)
        {
            int slot = i;
            buyActions[i] = () => OnBuyClicked(slot);

            if (buyButtons[i] != null)
                buyButtons[i].onClick.AddListener(buyActions[i]);
        }

        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshClicked);
    }

    private void Update()
    {
        if (manager == null)
            manager = BoardGameManager.Instance;

        bool myShop = CanUseShop();

        if (shopPanel != null && shopPanel.activeSelf != myShop)
            shopPanel.SetActive(myShop);

        if (!myShop)
            return;

        UpdateCountdown();
        UpdateSkillCards();
        UpdateRefreshButton();
    }

    private void UpdateCountdown()
    {
        if (countdownText == null)
            return;

        countdownText.text =
            "SHOP CLOSES IN: " +
            Mathf.CeilToInt(manager.ShopSecondsLeft.Value);
    }

    private void UpdateSkillCards()
    {
        int player = manager.GetLocalPlayerNumber();

        int mana = player == 0
            ? manager.Player1Mana.Value
            : manager.Player2Mana.Value;

        for (int i = 0; i < 3; i++)
        {
            int skillId = manager.GetShopOffer(i);
            int cost = BoardGameManager.GetSkillCost(skillId);

            if (i < skillNames.Length && skillNames[i] != null)
            {
                skillNames[i].text = skillId >= 0
                    ? BoardGameManager.GetSkillName(skillId)
                    : "No Skill";
            }

            if (i < skillPrices.Length && skillPrices[i] != null)
            {
                skillPrices[i].text = cost >= 0
                    ? cost + " Mana"
                    : "";
            }

            if (i < buyButtons.Length && buyButtons[i] != null)
            {
                buyButtons[i].interactable =
                    skillId >= 0 &&
                    cost >= 0 &&
                    mana >= cost;
            }
        }
    }

    private void UpdateRefreshButton()
    {
        if (refreshButton == null)
            return;

        int player = manager.GetLocalPlayerNumber();

        int mana = player == 0
            ? manager.Player1Mana.Value
            : manager.Player2Mana.Value;

        refreshButton.interactable = mana >= 2;
    }

    private void OnBuyClicked(int slot)
    {
        if (!CanUseShop())
            return;

        if (slot < 0 || slot >= 3)
            return;

        manager.BuyShopSkillRpc(slot);
    }

    private void OnRefreshClicked()
    {
        if (!CanUseShop())
            return;

        manager.RefreshShopRpc();
    }

    private void OnSkipClicked()
    {
        if (!CanUseShop())
            return;

        manager.SkipShopRpc();
    }

    private bool CanUseShop()
    {
        return manager != null &&
               manager.IsSpawned &&
               manager.ShopOpen.Value &&
               manager.IsMyTurn();
    }

    private void OnDestroy()
    {
        if (skipButton != null)
            skipButton.onClick.RemoveListener(OnSkipClicked);

        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(OnRefreshClicked);

        if (buyActions != null)
        {
            for (int i = 0; i < buyActions.Length; i++)
            {
                if (i < buyButtons.Length &&
                    buyButtons[i] != null &&
                    buyActions[i] != null)
                {
                    buyButtons[i].onClick.RemoveListener(buyActions[i]);
                }
            }
        }
    }
}
