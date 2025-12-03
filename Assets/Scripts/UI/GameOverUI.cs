using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Controls the Game Over panel and its buttons.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("ui objects")]
    public GameObject panelRoot;
    public GameObject extraLifeButton;
    public TMP_Text extraLifeCountText;   // optional

    [Header("optional")]
    public CanvasGroup canvasGroup;       // optional CanvasGroup on panelRoot

    PlayerLifeSystem lifeSystem;

    void Awake()
    {
        lifeSystem = FindObjectOfType<PlayerLifeSystem>();

        if (canvasGroup == null && panelRoot != null)
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();

        // start hidden + non-interactable
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Show game over panel.
    /// canUseExtraLife = whether the extra life button is visible.
    /// </summary>
    public void ShowGameOver(bool canUseExtraLife, int extraLivesRemaining)
    {
        Debug.Log($"[GameOverUI] ShowGameOver, canUseExtraLife = {canUseExtraLife}, lives = {extraLivesRemaining}");

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (extraLifeButton != null)
            extraLifeButton.SetActive(canUseExtraLife);

        if (extraLifeCountText != null)
            extraLifeCountText.text = "Extra Lives: " + extraLivesRemaining;

        // set keyboard focus on one of the buttons (just to be safe)
        if (EventSystem.current != null)
        {
            GameObject focus = canUseExtraLife && extraLifeButton != null
                ? extraLifeButton
                : panelRoot;

            EventSystem.current.SetSelectedGameObject(focus);
        }
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0f;
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    // Hook this to the "Use Extra Life" button OnClick
    public void OnUseExtraLifePressed()
    {
        Debug.Log("[GameOverUI] UseExtraLife button pressed.");

        if (lifeSystem != null)
            lifeSystem.UseExtraLife();

        Hide();
    }

    // Hook this to the Restart / BackToGarage button OnClick
    public void OnRestartToGaragePressed()
    {
        Debug.Log("[GameOverUI] Restart/BackToGarage button pressed.");
        Time.timeScale = 1f;   // make sure time is normal when reloading
        SceneManager.LoadScene("Garage Scene");
    }
}
