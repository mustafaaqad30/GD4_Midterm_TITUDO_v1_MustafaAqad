using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System.Net.Sockets;

public class NetworkUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;

    private NetworkManager networkManager;
    private UnityTransport transport;
    private string hostAddress;

    // Use the scene's transport address as the editable initial destination.
    private void OnGUI()
    {
        if (transport == null || networkManager.IsListening)
            return;

        GUILayout.BeginArea(new Rect(12, 12, 300, 100), GUI.skin.box);
        GUILayout.Label("Host IPv4 address (Client)");
        hostAddress = GUILayout.TextField(hostAddress ?? string.Empty, 45);
        if (GUILayout.Button("Same computer: 127.0.0.1"))
            hostAddress = "127.0.0.1";
        GUILayout.EndArea();
    }

    private void Start()
    {
        networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            Debug.LogError("NETWORK MANAGER NOT FOUND!");
            return;
        }

        transport = networkManager.NetworkConfig.NetworkTransport as UnityTransport;
        if (transport == null)
        {
            statusText.text = "Unity Transport is required.";
            DisableButtons();
            return;
        }

        hostAddress = transport.ConnectionData.Address;
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);

        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;

        statusText.text = "Not Connected";
    }

    private void PrintNetworkConfig(string side)
    {
        NetworkConfig config = networkManager.NetworkConfig;

        Debug.Log("======================================");
        Debug.Log("NETWORK CONFIG - " + side);
        Debug.Log("ProtocolVersion: " + config.ProtocolVersion);
        Debug.Log("TickRate: " + config.TickRate);
        Debug.Log("ClientConnectionBufferTimeout: " +
                  config.ClientConnectionBufferTimeout);
        Debug.Log("ConnectionApproval: " +
                  config.ConnectionApproval);
        Debug.Log("EnableSceneManagement: " +
                  config.EnableSceneManagement);
        Debug.Log("ForceSamePrefabs: " +
                  config.ForceSamePrefabs);
        Debug.Log("EnsureNetworkVariableLengthSafety: " +
                  config.EnsureNetworkVariableLengthSafety);
        Debug.Log("RpcHashSize: " +
                  config.RpcHashSize);
        Debug.Log("NetworkTransport: " +
                  config.NetworkTransport.GetType().Name);

        UnityTransport transport =
            config.NetworkTransport as UnityTransport;

        if (transport != null)
        {
            Debug.Log("Transport Protocol: " +
                      transport.Protocol);
        }

        Debug.Log("======================================");
    }

    private void StartHost()
    {
        if (networkManager.IsListening || networkManager.ShutdownInProgress)
            return;

        // Listen on LAN and loopback; the client destination must not restrict the host.
        transport.SetConnectionData("127.0.0.1", transport.ConnectionData.Port, "0.0.0.0");
        PrintNetworkConfig("HOST");

        bool result = networkManager.StartHost();

        Debug.Log("StartHost: " + result);

        if (result)
        {
            statusText.text =
                "Host Started - Waiting for Player 2...";

            DisableButtons();
        }
    }

    private void StartClient()
    {
        if (networkManager.IsListening || networkManager.ShutdownInProgress)
            return;

        string address = (hostAddress ?? string.Empty).Trim();
        if (!IPAddress.TryParse(address, out IPAddress parsedAddress) ||
            parsedAddress.AddressFamily != AddressFamily.InterNetwork ||
            parsedAddress.Equals(IPAddress.Any) || parsedAddress.Equals(IPAddress.Broadcast) ||
            parsedAddress.GetAddressBytes()[0] >= 224)
        {
            statusText.text = "Enter a valid Host IPv4 address.";
            return;
        }

        hostAddress = parsedAddress.ToString();
        transport.SetConnectionData(hostAddress, transport.ConnectionData.Port);
        PrintNetworkConfig("CLIENT");

        bool result = networkManager.StartClient();

        Debug.Log("StartClient: " + result);

        if (result)
        {
            statusText.text = $"Connecting to {hostAddress}:{transport.ConnectionData.Port}...";
            DisableButtons();
        }
        else
        {
            statusText.text = "Client could not start. Check the Console.";
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log("CLIENT CONNECTED: " + clientId);

        if (networkManager.IsHost)
        {
            statusText.text =
                $"Players Connected: {networkManager.ConnectedClients.Count}/2";
        }
        else
        {
            statusText.text = "Connected to Host!";
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.LogWarning(
            "CLIENT DISCONNECTED: " + clientId +
            " | Reason: " + networkManager.DisconnectReason
        );

        if (!networkManager.IsHost)
        {
            statusText.text =
                "Disconnected: " +
                networkManager.DisconnectReason;

            EnableButtons();
        }
    }

    private void DisableButtons()
    {
        hostButton.interactable = false;
        clientButton.interactable = false;
    }

    private void EnableButtons()
    {
        hostButton.interactable = true;
        clientButton.interactable = true;
    }

    private void OnDestroy()
    {
        if (networkManager == null)
            return;

        networkManager.OnClientConnectedCallback -=
            OnClientConnected;

        networkManager.OnClientDisconnectCallback -=
            OnClientDisconnected;
    }
}
