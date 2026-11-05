using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class OnlinePlayerCamera : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera playerCamera;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 0.03f;
    [SerializeField] private float lookSmoothness = 10f;
    [SerializeField] private float maxLookAngle = 70f;

    private float targetPitch;
    private float targetYaw;

    private float smoothPitch;
    private float smoothYaw;

    // تحفظ زاوية Y الأصلية لكل كاميرا
    private float cameraBaseYaw;

    private bool isLocalPlayerCamera = false;
    private bool setupComplete = false;

    private void Start()
    {
        // IMPORTANT:
        // نحفظ زاوية الكاميرا الأصلية قبل أي تعديل
        if (playerCamera != null)
        {
            cameraBaseYaw =
                playerCamera.transform.localEulerAngles.y;

            playerCamera.enabled = false;

            AudioListener listener =
                playerCamera.GetComponent<AudioListener>();

            if (listener != null)
            {
                listener.enabled = false;
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (!setupComplete)
        {
            TrySetupCamera();
            return;
        }

        if (!isLocalPlayerCamera)
            return;

        if (playerCamera == null)
            return;

        if (Mouse.current == null)
            return;

        if (BoardGameManager.Instance == null)
            return;

        // أثناء اختيار Target
        // الكاميرا لا تتحرك
        if (!BoardGameManager.Instance.TargetSelected.Value)
            return;

        // إذا الماوس فوق UI
        // لا نحرك الكاميرا
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue();

        // يمين / يسار
        targetYaw +=
            mouseDelta.x *
            mouseSensitivity;

        // فوق / تحت
        targetPitch -=
            mouseDelta.y *
            mouseSensitivity;

        targetPitch = Mathf.Clamp(
            targetPitch,
            -maxLookAngle,
            maxLookAngle
        );

        // Smooth
        smoothYaw = Mathf.LerpAngle(
            smoothYaw,
            targetYaw,
            lookSmoothness * Time.deltaTime
        );

        smoothPitch = Mathf.LerpAngle(
            smoothPitch,
            targetPitch,
            lookSmoothness * Time.deltaTime
        );

        // يمين / يسار على Player
        transform.rotation =
            Quaternion.Euler(
                0f,
                smoothYaw,
                0f
            );

        // فوق / تحت على Camera
        // مع الحفاظ على Y الأصلي
        playerCamera.transform.localRotation =
            Quaternion.Euler(
                smoothPitch,
                cameraBaseYaw,
                0f
            );
    }

    private void TrySetupCamera()
    {
        if (BoardGameManager.Instance == null)
            return;

        int localPlayer =
            BoardGameManager.Instance.GetLocalPlayerNumber();

        if (localPlayer == -1)
            return;

        bool thisIsPlayer1 =
            gameObject.name == "Player1" || gameObject.name == "Player 1";

        bool thisIsPlayer2 =
            gameObject.name == "Player2" || gameObject.name == "Player 2";

        isLocalPlayerCamera =
            (localPlayer == 0 && thisIsPlayer1) ||
            (localPlayer == 1 && thisIsPlayer2);

        if (playerCamera != null)
        {
            playerCamera.enabled =
                isLocalPlayerCamera;

            AudioListener listener =
                playerCamera.GetComponent<AudioListener>();

            if (listener != null)
            {
                listener.enabled =
                    isLocalPlayerCamera;
            }
        }

        setupComplete = true;

        if (!isLocalPlayerCamera)
            return;

        // زاوية Player الأصلية
        targetYaw =
            transform.eulerAngles.y;

        smoothYaw =
            targetYaw;

        // زاوية X الأصلية للكاميرا
        float cameraX =
            playerCamera.transform.localEulerAngles.x;

        if (cameraX > 180f)
        {
            cameraX -= 360f;
        }

        targetPitch =
            cameraX;

        smoothPitch =
            targetPitch;

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        Debug.Log(
            "Local camera activated for Player " +
            (localPlayer + 1) +
            " | Camera Base Y: " +
            cameraBaseYaw
        );
    }
}
