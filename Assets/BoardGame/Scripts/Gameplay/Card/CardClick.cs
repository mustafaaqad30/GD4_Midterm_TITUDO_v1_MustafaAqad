using UnityEngine;

public class CardClick : MonoBehaviour
{
    private int ownerPlayer = -1;

    public void Setup(int playerNumber)
    {
        ownerPlayer = playerNumber;
    }

    private void OnMouseDown()
    {
        if (BoardGameManager.Instance == null)
            return;

        if (!BoardGameManager.Instance.GameStarted.Value)
            return;

        if (!BoardGameManager.Instance.TargetSelected.Value)
            return;

        // السيرفر نفسه سيتأكد من هوية اللاعب.
        // هنا نحتاج فقط أن يكون دور اللاعب المحلي.
        if (!BoardGameManager.Instance.IsMyTurn())
        {
            Debug.Log("Not your turn.");
            return;
        }

        Debug.Log(
            "CARD CLICKED -> DRAW | Visual Owner: Player " +
            (ownerPlayer + 1)
        );

        BoardGameManager.Instance.DrawCardRpc();
    }
}