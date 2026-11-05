
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Netcode;

public class SkillCardVisualManager : MonoBehaviour
{
    [Header("Skill Card Prefab")]
    [SerializeField] private GameObject skillCardPrefab;

    [Header("Skill Card Holders")]
    [SerializeField] private Transform player1SkillsHolder;
    [SerializeField] private Transform player2SkillsHolder;

    [Header("Card Layout")]
    [SerializeField] private float cardSpacing = 0.85f;

    private readonly List<GameObject> player1Cards = new();
    private readonly List<GameObject> player2Cards = new();

    private readonly List<int> previousPlayer1Skills = new();
    private readonly List<int> previousPlayer2Skills = new();

    private void Update()
    {
        BoardGameManager game = BoardGameManager.Instance;

        if (game == null || !game.IsSpawned)
            return;

        if (game.GetLocalPlayerNumber() == -1)
            return;

        SyncSkills(
            game.Player1OwnedSkills,
            previousPlayer1Skills,
            player1Cards,
            player1SkillsHolder,
            0
        );

        SyncSkills(
            game.Player2OwnedSkills,
            previousPlayer2Skills,
            player2Cards,
            player2SkillsHolder,
            1
        );
    }

    private void SyncSkills(
        NetworkList<int> ownedSkills,
        List<int> previousSkills,
        List<GameObject> visualCards,
        Transform holder,
        int playerNumber)
    {
        List<int> currentSkills = new List<int>();

        for (int i = 0; i < ownedSkills.Count; i++)
        {
            currentSkills.Add(ownedSkills[i]);
        }

        bool unchanged =
            currentSkills.Count == previousSkills.Count;

        if (unchanged)
        {
            for (int i = 0; i < currentSkills.Count; i++)
            {
                if (currentSkills[i] != previousSkills[i])
                {
                    unchanged = false;
                    break;
                }
            }
        }

        if (unchanged)
            return;

        foreach (GameObject card in visualCards)
        {
            if (card != null)
                Destroy(card);
        }

        visualCards.Clear();

        if (holder == null || skillCardPrefab == null)
        {
            Debug.LogError(
                "SKILL VISUAL ERROR | Missing Prefab or Holder!"
            );
            return;
        }

        // Create skill cards using local coordinates
        for (int i = 0; i < currentSkills.Count; i++)
        {
            int skillId = currentSkills[i];

            GameObject card = Instantiate(
                skillCardPrefab,
                holder
            );

            // Keep the card positioned relative to its holder
            card.transform.localPosition =
                Vector3.right * (i * cardSpacing);

            card.transform.localRotation =
                Quaternion.identity;

            card.transform.localScale =
                skillCardPrefab.transform.localScale;

            card.name =
                "Player" + (playerNumber + 1) +
                "_Skill_" + i;

            // Set skill name
            TMP_Text skillText =
                card.GetComponentInChildren<TMP_Text>();

            if (skillText != null)
            {
                skillText.text =
                    BoardGameManager.GetSkillName(skillId);
            }
            else
            {
                Debug.LogWarning(
                    "SKILL VISUAL WARNING | TextMeshPro missing!"
                );
            }

            // Set up clicking information
            SkillCardClick skillClick =
                card.GetComponent<SkillCardClick>();

            if (skillClick != null)
            {
                skillClick.Setup(
                    playerNumber,
                    i,
                    skillId
                );
            }
            else
            {
                Debug.LogWarning(
                    "SkillCardClick missing from SkillCard3D!"
                );
            }

            visualCards.Add(card);

            Debug.Log(
                "SKILL CARD CREATED | Player: " +
                (playerNumber + 1) +
                " | Skill: " +
                BoardGameManager.GetSkillName(skillId)
            );
        }

        previousSkills.Clear();
        previousSkills.AddRange(currentSkills);
    }

    private void OnDestroy()
    {
        foreach (GameObject card in player1Cards)
        {
            if (card != null)
                Destroy(card);
        }

        foreach (GameObject card in player2Cards)
        {
            if (card != null)
                Destroy(card);
        }

        player1Cards.Clear();
        player2Cards.Clear();
    }
}
