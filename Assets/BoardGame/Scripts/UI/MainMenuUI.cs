
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void PlayOnline()
    {
        SceneManager.LoadScene("Lobby");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
