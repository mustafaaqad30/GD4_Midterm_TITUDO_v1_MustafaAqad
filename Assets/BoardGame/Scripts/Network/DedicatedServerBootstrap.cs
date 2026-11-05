
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class DedicatedServerBootstrap : MonoBehaviour
{
    [SerializeField] private ushort serverPort = 7777;

    private void Start()
    {
        // Only run automatically in a Dedicated Server build.
        if (!Application.isBatchMode)
            return;

        NetworkManager manager = NetworkManager.Singleton;

        if (manager == null)
        {
            Debug.LogError("Dedicated Server: NetworkManager not found!");
            return;
        }

        if (manager.IsListening)
            return;

        UnityTransport transport =
            manager.NetworkConfig.NetworkTransport as UnityTransport;

        if (transport == null)
        {
            Debug.LogError("Dedicated Server: Unity Transport not found!");
            return;
        }

        // Listen on all available network interfaces.
        transport.SetConnectionData(
            "127.0.0.1",
            serverPort,
            "0.0.0.0"
        );

        bool started = manager.StartServer();

        if (started)
        {
            Debug.Log(
                "DEDICATED SERVER STARTED | Port: " + serverPort
            );
        }
        else
        {
            Debug.LogError("DEDICATED SERVER FAILED TO START!");
        }
    }
}
