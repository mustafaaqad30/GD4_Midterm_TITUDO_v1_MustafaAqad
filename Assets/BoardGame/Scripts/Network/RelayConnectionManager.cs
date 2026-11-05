
using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class RelayConnectionManager : MonoBehaviour
{
    public static RelayConnectionManager Instance { get; private set; }

    public string JoinCode { get; private set; } = "";
    public string Status { get; private set; } = "Not connected";

    public event Action<string> OnStatusChanged;
    public event Action<string> OnJoinCodeChanged;

    private NetworkManager networkManager;
    private UnityTransport transport;
    private bool isBusy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Duplicate RelayConnectionManager detected.");
            enabled = false;
            return;
        }

        Instance = this;

        // Keep the NetworkManager alive between scenes.
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            SetStatus("NetworkManager not found.");
            return;
        }

        transport = networkManager.NetworkConfig.NetworkTransport
            as UnityTransport;

        if (transport == null)
        {
            SetStatus("UnityTransport not found.");
            return;
        }

        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public async void CreateRoom()
    {
        await CreateRoomAsync();
    }

    public async void JoinRoom(string code)
    {
        await JoinRoomAsync(code);
    }

    private async Task InitializeServicesAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private async Task CreateRoomAsync()
    {
        if (!CanStartConnection())
            return;

        isBusy = true;
        SetStatus("Creating room...");

        try
        {
            await InitializeServicesAsync();

            // One additional player can join the host.
            Allocation allocation =
                await RelayService.Instance.CreateAllocationAsync(1);

            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, "dtls")
            );

            JoinCode =
                await RelayService.Instance.GetJoinCodeAsync(
                    allocation.AllocationId
                );

            OnJoinCodeChanged?.Invoke(JoinCode);

            bool started = networkManager.StartHost();

            if (!started)
            {
                JoinCode = "";
                OnJoinCodeChanged?.Invoke("");
                SetStatus("Failed to start host.");
                return;
            }

            SetStatus("Room created. Code: " + JoinCode);
            Debug.Log("RELAY ROOM CREATED: " + JoinCode);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            SetStatus("Create room failed: " + exception.Message);
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task JoinRoomAsync(string code)
    {
        if (!CanStartConnection())
            return;

        code = (code ?? "").Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(code))
        {
            SetStatus("Enter a room code.");
            return;
        }

        isBusy = true;
        SetStatus("Joining room...");

        try
        {
            await InitializeServicesAsync();

            JoinAllocation allocation =
                await RelayService.Instance.JoinAllocationAsync(code);

            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, "dtls")
            );

            bool started = networkManager.StartClient();

            if (!started)
            {
                SetStatus("Failed to start client.");
                return;
            }

            SetStatus("Connecting to room...");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            SetStatus("Join room failed: " + exception.Message);
        }
        finally
        {
            isBusy = false;
        }
    }

    private bool CanStartConnection()
    {
        if (isBusy)
        {
            SetStatus("Please wait...");
            return false;
        }

        if (networkManager == null || transport == null)
        {
            SetStatus("Network system is not ready.");
            return false;
        }

        if (networkManager.IsListening ||
            networkManager.ShutdownInProgress)
        {
            SetStatus("Already connected or shutting down.");
            return false;
        }

        return true;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (networkManager.IsHost)
        {
            SetStatus(
                "Players connected: " +
                networkManager.ConnectedClients.Count + "/2"
            );
        }
        else if (clientId == networkManager.LocalClientId)
        {
            SetStatus("Connected to room!");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (networkManager == null)
            return;

        if (networkManager.IsHost)
        {
            SetStatus(
                "Player disconnected. Players: " +
                networkManager.ConnectedClients.Count + "/2"
            );
        }
        else if (!networkManager.IsListening)
        {
            SetStatus(
                "Disconnected: " + networkManager.DisconnectReason
            );
        }
    }

    private void SetStatus(string message)
    {
        Status = message;
        OnStatusChanged?.Invoke(message);
        Debug.Log("[Relay] " + message);
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        if (Instance == this)
            Instance = null;
    }
}
