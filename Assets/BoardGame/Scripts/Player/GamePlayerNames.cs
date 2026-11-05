using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GamePlayerNames : NetworkBehaviour
{
    public static GamePlayerNames Instance { get; private set; }

    public NetworkVariable<FixedString128Bytes> Player1Name =
        new(new FixedString128Bytes(""));

    public NetworkVariable<FixedString128Bytes> Player2Name =
        new(new FixedString128Bytes(""));

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Player1Name.Value = new FixedString128Bytes("");
            Player2Name.Value = new FixedString128Bytes("");
        }

        SubmitNameRpc(PlayerSession.PlayerName);
    }

    [Rpc(SendTo.Server)]
    private void SubmitNameRpc(string name, RpcParams rpcParams = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        name = name.Trim();

        if (name.Length > 50)
            name = name.Substring(0, 50);

        ulong senderId = rpcParams.Receive.SenderClientId;

        if (senderId == NetworkManager.ServerClientId)
            Player1Name.Value = new FixedString128Bytes(name);
        else
            Player2Name.Value = new FixedString128Bytes(name);
    }

    public string GetPlayerName(int player)
    {
        string name = player == 0
            ? Player1Name.Value.ToString()
            : Player2Name.Value.ToString();

        return string.IsNullOrWhiteSpace(name)
            ? $"Player {player + 1}"
            : name;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}