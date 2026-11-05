using UnityEngine;
using UnityEngine.EventSystems;

public class TableClick : MonoBehaviour
{
    private void OnMouseDown()
    {
        // إذا كبسنا على UI لا نعتبرها كبسة على الطاولة
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (BoardGameManager.Instance == null)
        {
            Debug.LogWarning("BoardGameManager not found.");
            return;
        }

        BoardGameManager game =
            BoardGameManager.Instance;

        if (!game.GameStarted.Value)
        {
            Debug.Log("Game has not started yet.");
            return;
        }

        if (!game.TargetSelected.Value)
        {
            Debug.Log("Target has not been selected yet.");
            return;
        }

        if (!game.IsMyTurn())
        {
            Debug.Log("Not your turn.");
            return;
        }

        Debug.Log("TABLE CLICKED -> PASS");

        game.PassTurnRpc();
    }
}