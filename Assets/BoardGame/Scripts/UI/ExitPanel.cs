
using UnityEngine;
using UnityEngine.UI;

public class ExitPanel : MonoBehaviour
{
    [Header("Panel Settings")]
    [SerializeField] private GameObject panel;

    [Header("Exit Button")]
    [SerializeField] private Button exitButton;

    private void Start()
    {
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ClosePanel);
        }
    }

    public void ClosePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ClosePanel);
        }
    }
}
