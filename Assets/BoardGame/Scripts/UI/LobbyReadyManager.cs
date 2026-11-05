
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class LobbyReadyManager : NetworkBehaviour
{
    public static LobbyReadyManager Instance { get; private set; }

    [Header("Ready Status")]
    public NetworkVariable<bool> hostReady = new(false);
    public NetworkVariable<bool> clientReady = new(false);

    [Header("Player Names")]
    public NetworkVariable<FixedString128Bytes> hostName =
        new(new FixedString128Bytes(""));

    public NetworkVariable<FixedString128Bytes> clientName =
        new(new FixedString128Bytes(""));

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            hostReady.Value = false;
            clientReady.Value = false;

            hostName.Value = new FixedString128Bytes("");
            clientName.Value = new FixedString128Bytes("");

            NetworkManager.OnClientDisconnectCallback +=
                HandleClientDisconnected;
        }

        Debug.Log(
            "[NAME DEBUG] LobbyReadyManager spawned. " +
            "IsServer: " + IsServer +
            " | IsClient: " + IsClient
        );

        SubmitLocalName();
    }

    private void SubmitLocalName()
    {
        string name = PlayerSession.PlayerName;

        Debug.Log(
            "[NAME DEBUG] Local name = '" + name + "'"
        );

        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogError(
                "[NAME DEBUG] PlayerSession is EMPTY! " +
                "Name was not transferred from MainMenu."
            );

            return;
        }

        Debug.Log(
            "[NAME DEBUG] Sending name to server: " + name
        );

        SubmitNameRpc(name);
    }

    [Rpc(SendTo.Server)]
    private void SubmitNameRpc(
        string name,
        RpcParams rpcParams = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        name = name.Trim();

        if (name.Length > 50)
            name = name.Substring(0, 50);

        ulong senderId =
            rpcParams.Receive.SenderClientId;

        Debug.Log(
            "[NAME DEBUG] Server received name: " +
            name + " from ClientId: " + senderId
        );

        if (senderId == NetworkManager.ServerClientId)
        {
            hostName.Value =
                new FixedString128Bytes(name);

            Debug.Log(
                "[NAME DEBUG] Host name saved: " +
                hostName.Value
            );
        }
        else
        {
            clientName.Value =
                new FixedString128Bytes(name);

            Debug.Log(
                "[NAME DEBUG] Client name saved: " +
                clientName.Value
            );
        }
    }

    public void SetReady(bool ready)
    {
        if (!IsSpawned)
            return;

        SetReadyRpc(ready);
    }

    [Rpc(SendTo.Server)]
    private void SetReadyRpc(
        bool ready,
        RpcParams rpcParams = default)
    {
        ulong senderId =
            rpcParams.Receive.SenderClientId;

        if (senderId == NetworkManager.ServerClientId)
        {
            hostReady.Value = ready;
        }
        else
        {
            clientReady.Value = ready;
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (!IsServer)
            return;

        if (clientId != NetworkManager.ServerClientId)
        {
            clientReady.Value = false;
            clientName.Value =
                new FixedString128Bytes("");

            Debug.Log(
                "[NAME DEBUG] Client disconnected. " +
                "Name and ready status cleared."
            );
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -=
                HandleClientDisconnected;
        }

        if (Instance == this)
            Instance = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
