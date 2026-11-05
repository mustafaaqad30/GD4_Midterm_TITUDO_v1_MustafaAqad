
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RelayUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    [Header("Room Code")]
    [SerializeField] private TMP_InputField roomCodeInput;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    private RelayConnectionManager relayManager;

    private void Start()
    {
        relayManager = RelayConnectionManager.Instance;

        if (relayManager == null)
        {
            Debug.LogError("RelayUI: RelayConnectionManager not found!");

            if (statusText != null)
                statusText.text = "Relay manager not found.";

            return;
        }

        if (hostButton == null ||
            clientButton == null ||
            roomCodeInput == null ||
            statusText == null)
        {
            Debug.LogError("RelayUI: Missing UI references!");
            return;
        }

        hostButton.onClick.AddListener(CreateRoom);
        clientButton.onClick.AddListener(JoinRoom);

        relayManager.OnStatusChanged += UpdateStatus;
        relayManager.OnJoinCodeChanged += UpdateJoinCode;

        UpdateStatus(relayManager.Status);
    }

    private void CreateRoom()
    {
        relayManager.CreateRoom();
    }

    private void JoinRoom()
    {
        string code = roomCodeInput.text.Trim();

        if (string.IsNullOrWhiteSpace(code))
        {
            UpdateStatus("Please enter a room code.");
            return;
        }

        relayManager.JoinRoom(code);
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void UpdateJoinCode(string code)
    {
        if (string.IsNullOrEmpty(code))
            return;

        if (statusText != null)
            statusText.text = "Room Code: " + code;

        Debug.Log("ROOM CODE: " + code);
    }

    private void OnDestroy()
    {
        if (hostButton != null)
            hostButton.onClick.RemoveListener(CreateRoom);

        if (clientButton != null)
            clientButton.onClick.RemoveListener(JoinRoom);

        if (relayManager != null)
        {
            relayManager.OnStatusChanged -= UpdateStatus;
            relayManager.OnJoinCodeChanged -= UpdateJoinCode;
        }
    }
}
