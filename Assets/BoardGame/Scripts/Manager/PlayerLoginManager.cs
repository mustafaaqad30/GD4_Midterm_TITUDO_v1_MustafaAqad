
using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLoginManager : MonoBehaviour
{
    public static PlayerLoginManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject namePanel;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Login")]
    [SerializeField] private TMP_Text loginStatusText;
    [SerializeField] private Button unityLoginButton;

    [Header("Name")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Text nameStatusText;
    [SerializeField] private Button doneButton;
    [SerializeField] private Button randomNameButton;

    public string PlayerName { get; private set; } = "";

    public string PlayerId =>
        AuthenticationService.Instance.PlayerId;

    private bool busy;
    private bool initialized;
    private bool loginCompleted;

    private void Awake()
    {
        Instance = this;
    }

    private async void Start()
    {
        PlayerSession.Clear();

        ShowOnly(loginPanel);

        if (loginStatusText != null)
            loginStatusText.text = "Initializing...";

        if (unityLoginButton != null)
        {
            unityLoginButton.interactable = false;
            unityLoginButton.onClick.AddListener(
                OnUnityLoginClicked
            );
        }

        if (doneButton != null)
        {
            doneButton.onClick.AddListener(
                OnDoneClicked
            );
        }

        if (randomNameButton != null)
        {
            randomNameButton.onClick.AddListener(
                GenerateRandomName
            );
        }

        try
        {
            if (UnityServices.State !=
                ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            PlayerAccountService.Instance.SignedIn +=
                OnUnityAccountSignedIn;

            initialized = true;

            // Restore an existing Authentication session.
            if (AuthenticationService.Instance.IsSignedIn)
            {
                await CheckPlayerNameAsync();
            }
            else if (
                AuthenticationService.Instance.SessionTokenExists)
            {
                await RestoreSessionAsync();
            }
            else
            {
                ShowLogin();
            }
        }
        catch (Exception e)
        {
            ShowLoginError(e);
        }
    }

    private async Task RestoreSessionAsync()
    {
        try
        {
            SetLoginStatus("Restoring session...");

            await AuthenticationService.Instance
                .SignInAnonymouslyAsync();

            await CheckPlayerNameAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "[Login] Could not restore session: " +
                e.Message
            );

            ShowLogin();
        }
    }

    private void OnUnityLoginClicked()
    {
        _ = LoginWithUnityAsync();
    }

    private async Task LoginWithUnityAsync()
    {
        if (!initialized || busy)
            return;

        busy = true;

        if (unityLoginButton != null)
            unityLoginButton.interactable = false;

        SetLoginStatus("Opening Unity login...");

        try
        {
            if (PlayerAccountService.Instance.IsSignedIn)
            {
                await CompleteUnitySignInAsync();
            }
            else
            {
                await PlayerAccountService.Instance
                    .StartSignInAsync();

                SetLoginStatus(
                    "Complete sign-in in your browser..."
                );
            }
        }
        catch (Exception e)
        {
            busy = false;
            ShowLoginError(e);
        }
    }

    private async void OnUnityAccountSignedIn()
    {
        await CompleteUnitySignInAsync();
    }

    private async Task CompleteUnitySignInAsync()
    {
        if (loginCompleted)
            return;

        try
        {
            SetLoginStatus("Signing in...");

            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }

            await AuthenticationService.Instance
                .SignInWithUnityAsync(
                    PlayerAccountService.Instance.AccessToken
                );

            loginCompleted = true;

            await CheckPlayerNameAsync();
        }
        catch (Exception e)
        {
            loginCompleted = false;
            ShowLoginError(e);
        }
        finally
        {
            busy = false;

            if (unityLoginButton != null)
                unityLoginButton.interactable = true;
        }
    }

    private async Task CheckPlayerNameAsync()
    {
        string savedName =
            await AuthenticationService.Instance
                .GetPlayerNameAsync(false);

        if (string.IsNullOrWhiteSpace(savedName))
        {
            PlayerName = "";

            ShowOnly(namePanel);

            if (nameStatusText != null)
            {
                nameStatusText.text =
                    "Choose your player name";
            }
        }
        else
        {
            PlayerName = savedName;
            OpenMainMenu();
        }
    }

    private void OnDoneClicked()
    {
        _ = SaveNameAsync();
    }

    private async Task SaveNameAsync()
    {
        if (busy || nameInput == null)
            return;

        string requestedName = nameInput.text.Trim();

        if (requestedName.Length < 3 ||
            requestedName.Length > 16 ||
            requestedName.Contains(" "))
        {
            if (nameStatusText != null)
            {
                nameStatusText.text =
                    "Use 3-16 characters without spaces.";
            }

            return;
        }

        busy = true;

        if (doneButton != null)
            doneButton.interactable = false;

        if (nameStatusText != null)
            nameStatusText.text = "Saving name...";

        try
        {
            PlayerName =
                await AuthenticationService.Instance
                    .UpdatePlayerNameAsync(requestedName);

            OpenMainMenu();
        }
        catch (Exception e)
        {
            Debug.LogException(e);

            if (nameStatusText != null)
            {
                nameStatusText.text =
                    "Could not save name. Try again.";
            }
        }
        finally
        {
            busy = false;

            if (doneButton != null)
                doneButton.interactable = true;
        }
    }

    private void GenerateRandomName()
    {
        if (nameInput == null)
            return;

        string[] first =
        {
            "Dark", "Shadow", "Iron",
            "Silent", "Last"
        };

        string[] second =
        {
            "Hand", "King", "Ace",
            "Wolf", "Ghost"
        };

        nameInput.text =
            first[UnityEngine.Random.Range(0, first.Length)] +
            second[UnityEngine.Random.Range(0, second.Length)] +
            UnityEngine.Random.Range(10, 99);
    }

    private void OpenMainMenu()
    {
        PlayerSession.SetPlayer(
            PlayerName,
            AuthenticationService.Instance.PlayerId
        );

        ShowOnly(mainMenuPanel);

        Debug.Log("[Login] Welcome: " + PlayerName);
    }

    private void ShowLogin()
    {
        ShowOnly(loginPanel);

        SetLoginStatus("Sign in with Unity to continue.");

        if (unityLoginButton != null)
            unityLoginButton.interactable = initialized;
    }

    private void ShowLoginError(Exception e)
    {
        Debug.LogException(e);

        ShowOnly(loginPanel);

        SetLoginStatus(
            "Login failed. Please try again."
        );

        if (unityLoginButton != null)
            unityLoginButton.interactable = initialized;

        busy = false;
    }

    private void SetLoginStatus(string message)
    {
        if (loginStatusText != null)
            loginStatusText.text = message;
    }

    private void ShowOnly(GameObject target)
    {
        if (loginPanel != null)
            loginPanel.SetActive(target == loginPanel);

        if (namePanel != null)
            namePanel.SetActive(target == namePanel);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(target == mainMenuPanel);
    }

    private void OnDestroy()
    {
        if (initialized)
        {
            PlayerAccountService.Instance.SignedIn -=
                OnUnityAccountSignedIn;
        }

        if (unityLoginButton != null)
        {
            unityLoginButton.onClick.RemoveListener(
                OnUnityLoginClicked
            );
        }

        if (doneButton != null)
        {
            doneButton.onClick.RemoveListener(
                OnDoneClicked
            );
        }

        if (randomNameButton != null)
        {
            randomNameButton.onClick.RemoveListener(
                GenerateRandomName
            );
        }

        if (Instance == this)
            Instance = null;
    }
}
