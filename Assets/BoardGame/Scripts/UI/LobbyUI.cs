
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [Header("Room")]
    public TMP_InputField roomCodeInput;
    public TMP_Text roomCodeText;
    public TMP_Text connectionStatusText;

    [Header("Players")]
    public TMP_Text player1StatusText;
    public TMP_Text player2StatusText;

    [Header("Buttons")]
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button readyButton;
    public Button startGameButton;

    [Header("Settings")]
    public string gameplaySceneName = "SampleScene";

    private RelayConnectionManager relayManager;
    private NetworkManager networkManager;
    private LobbyReadyManager readyManager;

    private bool localReady = false;

    private void Start()
    {
        relayManager = RelayConnectionManager.Instance;
        networkManager = NetworkManager.Singleton;
        readyManager = LobbyReadyManager.Instance;

        if (relayManager == null || networkManager == null)
        {
            Debug.LogError(
                "[LobbyUI] NetworkManager or RelayConnectionManager missing."
            );
            return;
        }

        relayManager.OnJoinCodeChanged += UpdateRoomCode;
        relayManager.OnStatusChanged += UpdateConnectionStatus;

        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;

        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(CreateRoom);

        if (joinRoomButton != null)
            joinRoomButton.onClick.AddListener(JoinRoom);

        if (readyButton != null)
            readyButton.onClick.AddListener(ToggleReady);

        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);

        UpdateRoomCode(relayManager.JoinCode);
        UpdateConnectionStatus(relayManager.Status);

        RefreshUI();
    }

    private void Update()
    {
        if (readyManager == null || !readyManager.IsSpawned)
            readyManager = LobbyReadyManager.Instance;

        RefreshUI();
    }

    public void CreateRoom()
    {
        if (relayManager == null)
            return;

        localReady = false;
        relayManager.CreateRoom();
    }

    public void JoinRoom()
    {
        if (relayManager == null || roomCodeInput == null)
            return;

        localReady = false;
        relayManager.JoinRoom(roomCodeInput.text);
    }

    private void ToggleReady()
    {
        if (networkManager == null ||
            !networkManager.IsConnectedClient ||
            readyManager == null ||
            !readyManager.IsSpawned)
        {
            return;
        }

        localReady = !localReady;

        readyManager.SetReady(localReady);

        RefreshUI();
    }

    private void StartGame()
    {
        if (networkManager == null ||
            !networkManager.IsHost ||
            readyManager == null ||
            !readyManager.IsSpawned)
        {
            return;
        }

        if (networkManager.ConnectedClients.Count != 2)
            return;

        if (!readyManager.hostReady.Value ||
            !readyManager.clientReady.Value)
        {
            return;
        }

        Debug.Log("[LobbyUI] Starting game...");

        networkManager.SceneManager.LoadScene(
            gameplaySceneName,
            LoadSceneMode.Single
        );
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log(
            "[LobbyUI] Client connected: " + clientId
        );

        RefreshUI();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log(
            "[LobbyUI] Client disconnected: " + clientId
        );

        if (networkManager != null &&
            clientId == networkManager.LocalClientId)
        {
            localReady = false;
        }

        RefreshUI();
    }

    private void UpdateRoomCode(string code)
    {
        if (roomCodeText == null)
            return;

        roomCodeText.text = string.IsNullOrEmpty(code)
            ? "Room Code: ------"
            : "Room Code: " + code;
    }

    private void UpdateConnectionStatus(string status)
    {
        if (connectionStatusText != null)
            connectionStatusText.text = status;
    }

    private void RefreshUI()
    {
        if (networkManager == null)
            return;

        bool connected = networkManager.IsConnectedClient;
        bool isHost = networkManager.IsHost;

        bool managerReady =
            readyManager != null &&
            readyManager.IsSpawned;

        bool hostReady =
            managerReady &&
            readyManager.hostReady.Value;

        bool clientReady =
            managerReady &&
            readyManager.clientReady.Value;

        string hostName = managerReady
            ? readyManager.hostName.Value.ToString()
            : "";

        string clientName = managerReady
            ? readyManager.clientName.Value.ToString()
            : "";

        // Ignore placeholder names.
        if (string.IsNullOrWhiteSpace(hostName) ||
            hostName == "Waiting...")
        {
            hostName = "Player 1";
        }

        if (string.IsNullOrWhiteSpace(clientName) ||
            clientName == "Waiting...")
        {
            clientName = "Player 2";
        }

        int playerCount = connected
            ? networkManager.ConnectedClientsIds.Count
            : 0;

        bool hostConnected = connected;
        bool clientConnected =
            connected && playerCount >= 2;

        // PLAYER 1 / HOST
        if (player1StatusText != null)
        {
            if (!hostConnected)
            {
                player1StatusText.text =
                    "Player 1: Waiting...";
            }
            else
            {
                player1StatusText.text =
                    hostName +
                    (hostReady
                        ? " - Ready"
                        : " - Connected");
            }
        }

        // PLAYER 2 / CLIENT
        if (player2StatusText != null)
        {
            if (!clientConnected)
            {
                player2StatusText.text =
                    "Player 2: Waiting...";
            }
            else
            {
                player2StatusText.text =
                    clientName +
                    (clientReady
                        ? " - Ready"
                        : " - Connected");
            }
        }

        // READY BUTTON
        if (readyButton != null)
        {
            readyButton.interactable =
                connected && managerReady;

            TMP_Text buttonText =
                readyButton.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
            {
                buttonText.text =
                    localReady ? "Not Ready" : "Ready";
            }
        }

        // START GAME BUTTON
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(isHost);

            startGameButton.interactable =
                isHost &&
                playerCount == 2 &&
                hostReady &&
                clientReady;
        }

        // CREATE / JOIN BUTTONS
        if (createRoomButton != null)
            createRoomButton.interactable = !connected;

        if (joinRoomButton != null)
            joinRoomButton.interactable = !connected;
    }

    private void OnDestroy()
    {
        if (relayManager != null)
        {
            relayManager.OnJoinCodeChanged -= UpdateRoomCode;
            relayManager.OnStatusChanged -= UpdateConnectionStatus;
        }

        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        if (createRoomButton != null)
            createRoomButton.onClick.RemoveListener(CreateRoom);

        if (joinRoomButton != null)
            joinRoomButton.onClick.RemoveListener(JoinRoom);

        if (readyButton != null)
            readyButton.onClick.RemoveListener(ToggleReady);

        if (startGameButton != null)
            startGameButton.onClick.RemoveListener(StartGame);
    }
}
