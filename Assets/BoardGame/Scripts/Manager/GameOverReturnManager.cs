
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class GameOverReturnManager : NetworkBehaviour
{
    [Header("UI")]
    public Button returnButton;
    public TMP_Text returnButtonText;

    [Header("Settings")]
    public string lobbySceneName = "Lobby";

    private bool localRequested;

    private readonly NetworkVariable<bool> hostRequested =
        new(false);

    private readonly NetworkVariable<bool> clientRequested =
        new(false);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            hostRequested.Value = false;
            clientRequested.Value = false;
        }

        if (returnButton != null)
            returnButton.onClick.AddListener(RequestReturn);
    }

    private void Update()
    {
        if (!IsSpawned || returnButton == null)
            return;

        bool gameFinished =
            BoardGameManager.Instance != null &&
            BoardGameManager.Instance.GameFinished.Value;

        returnButton.interactable =
            gameFinished && !localRequested;

        if (returnButtonText != null && gameFinished)
        {
            returnButtonText.text = localRequested
                ? "Waiting for other player..."
                : "Return to Lobby";
        }
    }

    private void RequestReturn()
    {
        if (!IsSpawned || localRequested ||
            BoardGameManager.Instance == null ||
            !BoardGameManager.Instance.GameFinished.Value)
            return;

        localRequested = true;
        RequestReturnRpc();
    }

    [Rpc(SendTo.Server)]
    private void RequestReturnRpc(
        RpcParams rpcParams = default)
    {
        if (!IsServer ||
            BoardGameManager.Instance == null ||
            !BoardGameManager.Instance.GameFinished.Value)
            return;

        ulong sender = rpcParams.Receive.SenderClientId;

        if (sender ==
            NetworkManager.ServerClientId)
        {
            hostRequested.Value = true;
        }
        else if (NetworkManager.ConnectedClients.ContainsKey(sender))
        {
            clientRequested.Value = true;
        }
        else
        {
            return;
        }

        if (hostRequested.Value && clientRequested.Value &&
            NetworkManager.ConnectedClients.Count == 2)
        {
            NetworkManager.SceneManager.LoadScene(
                lobbySceneName,
                LoadSceneMode.Single
            );
        }
    }

    public override void OnNetworkDespawn()
    {
        if (returnButton != null)
            returnButton.onClick.RemoveListener(RequestReturn);

        base.OnNetworkDespawn();
    }
}
