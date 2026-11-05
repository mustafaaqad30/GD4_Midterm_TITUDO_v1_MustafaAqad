using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class SkillCardClick : MonoBehaviour
{
    private int ownerPlayer = -1;
    private int skillIndex = -1;
    private int skillId = -1;

    private static int lastClickFrame = -1;
    private static int selectedDestroyerIndex = -1;
    private static int selectingPlayer = -1;

    public void Setup(int owner, int index, int skill)
    {
        ownerPlayer = owner;
        skillIndex = index;
        skillId = skill;
    }

    private void Update()
    {
        if (Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (lastClickFrame == Time.frameCount)
            return;

        BoardGameManager game = BoardGameManager.Instance;
        if (game == null || !game.IsSpawned)
            return;

        int localPlayer = game.GetLocalPlayerNumber();
        if (localPlayer < 0)
            return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        foreach (Camera cam in Camera.allCameras)
        {
            if (cam == null || !cam.isActiveAndEnabled)
                continue;

            Ray ray = cam.ScreenPointToRay(mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(
                ray, 1000f, ~0, QueryTriggerInteraction.Collide);

            SkillCardClick nearestCard = null;
            float nearestDistance = float.PositiveInfinity;

            foreach (RaycastHit hit in hits)
            {
                SkillCardClick clickedCard =
                    hit.collider.GetComponentInParent<SkillCardClick>();

                if (clickedCard == null || hit.distance >= nearestDistance)
                    continue;

                nearestCard = clickedCard;
                nearestDistance = hit.distance;
            }

            if (nearestCard == this)
            {
                lastClickFrame = Time.frameCount;
                HandleClick(game, localPlayer);
                return;
            }
        }
    }

    private void HandleClick(BoardGameManager game, int localPlayer)
    {
        if (!game.GameStarted.Value ||
            !game.TargetSelected.Value ||
            game.RoundEnded.Value ||
            game.GameFinished.Value)
        {
            CancelSelection();
            return;
        }

        if (game.IsLocalPlayerBlocked())
        {
            CancelSelection();
            return;
        }

        if (selectedDestroyerIndex >= 0 &&
            selectingPlayer == localPlayer)
        {
            if (!game.IsMyTurn() || game.ShopOpen.Value)
            {
                CancelSelection();
            }
            else
            {
                if (ownerPlayer != localPlayer)
                {
                    Debug.Log(
                        "SKILL DESTROYER TARGET SELECTED | " +
                        BoardGameManager.GetSkillName(skillId));

                    game.DestroyOpponentSkillRpc(
                        selectedDestroyerIndex, skillIndex);

                    CancelSelection();
                    return;
                }

                if (skillIndex == selectedDestroyerIndex)
                {
                    CancelSelection();
                    Debug.Log("SKILL DESTROYER SELECTION CANCELLED");
                    return;
                }

                Debug.Log("SELECT AN OPPONENT SKILL CARD");
                return;
            }
        }

        if (ownerPlayer != localPlayer)
            return;

        if (skillId == (int)SkillType.TargetBreaker)
        {
            if (game.TargetNumber.Value <= 3)
                return;

            Debug.Log("TARGET BREAKER REQUESTED | Player: " +
                      (localPlayer + 1));
            game.UseTargetBreakerRpc(skillIndex);
            return;
        }

        if (skillId == (int)SkillType.DoubleDraw)
        {
            Debug.Log("DOUBLE DRAW REQUESTED | Player: " +
                      (localPlayer + 1));
            game.UseDoubleDrawRpc(skillIndex);
            return;
        }

        if (skillId == (int)SkillType.TurnBlock)
        {
            Debug.Log("TURN BLOCK REQUESTED | Player: " +
                      (localPlayer + 1));
            game.UseTurnBlockRpc(skillIndex);
            return;
        }

        if (skillId == (int)SkillType.TotalBooster)
        {
            Debug.Log("TOTAL BOOSTER REQUESTED | Player: " +
                      (localPlayer + 1));
            game.UseTotalBoosterRpc(skillIndex);
            return;
        }

        // Other skills require your turn and a closed shop.
        if (!game.IsMyTurn() || game.ShopOpen.Value)
            return;

        if (skillId == (int)SkillType.SecondChance)
        {
            Debug.Log("SECOND CHANCE REQUESTED | Player: " +
                      (localPlayer + 1));
            game.UseSecondChanceRpc(skillIndex);
            return;
        }

        Debug.Log(
            "SKILL CLICKED | Player: " + (ownerPlayer + 1) +
            " | Skill: " + BoardGameManager.GetSkillName(skillId) +
            " | Index: " + skillIndex);

        if (skillId == (int)SkillType.SkillDestroyer)
        {
            selectedDestroyerIndex = skillIndex;
            selectingPlayer = localPlayer;
            Debug.Log("SKILL DESTROYER READY | CLICK AN OPPONENT SKILL CARD");
        }
    }

    private static void CancelSelection()
    {
        selectedDestroyerIndex = -1;
        selectingPlayer = -1;
    }
}
