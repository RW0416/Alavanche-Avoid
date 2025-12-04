using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class CutsceneSkipManager : MonoBehaviour
{
    [Header("skip rules")]
    [Tooltip("playerprefs key to remember that this cutscene has been watched once fully")]
    public string cutsceneWatchedKey = "IntroCutsceneWatched";

    [Tooltip("you can only start skipping after this many seconds in the cutscene")]
    public float minTimeBeforeSkip = 3f;

    [Tooltip("how long E must be held to skip")]
    public float holdToSkipDuration = 3f;

    [Header("scene + fade")]
    [Tooltip("scene name to load after skipping (same as CameraSceneChange.sceneToLoad)")]
    public string sceneToLoad = "Game Scene";

    public CanvasGroup fadeCanvas;
    public float fadeDuration = 1f;

    [Header("ui")]
    public TextMeshProUGUI skipText;    // "press E to skip"
    public Image skipCircleImage;       // circle outline set to Filled / Radial

    bool hasWatchedOnce;
    bool canSkipThisRun;
    bool skipping;

    float cutsceneTime;
    float holdTimer;

    // used to know if this scene ended via skip or naturally
    public static bool SkippedThisRun = false;

    void Awake()
    {
        hasWatchedOnce = PlayerPrefs.GetInt(cutsceneWatchedKey, 0) == 1;
        canSkipThisRun = hasWatchedOnce;

        SkippedThisRun = false;

        // start with ui hidden
        if (skipText != null)
            skipText.gameObject.SetActive(false);

        if (skipCircleImage != null)
        {
            skipCircleImage.gameObject.SetActive(false);
            skipCircleImage.fillAmount = 0f;
        }

        if (fadeCanvas != null)
            fadeCanvas.alpha = 0f;
    }

    void Update()
    {
        if (!canSkipThisRun || skipping)
            return;

        cutsceneTime += Time.deltaTime;

        // not allowed to skip yet (first 3s)
        if (cutsceneTime < minTimeBeforeSkip)
        {
            HideUi();
            return;
        }

        var keyboard = Keyboard.current;
        bool holdingE = keyboard != null && keyboard.eKey.isPressed;

        if (!holdingE)
        {
            // show text, reset circle
            holdTimer = 0f;
            ShowText();
        }
        else
        {
            // show circle, fill over time
            ShowCircle();

            holdTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(holdTimer / holdToSkipDuration);

            if (skipCircleImage != null)
                skipCircleImage.fillAmount = progress;

            if (progress >= 1f)
            {
                StartCoroutine(SkipRoutine());
            }
        }
    }

    IEnumerator SkipRoutine()
    {
        if (skipping) yield break;
        skipping = true;
        SkippedThisRun = true;

        HideUi();

        float t = 0f;
        float startAlpha = fadeCanvas != null ? fadeCanvas.alpha : 0f;

        if (fadeCanvas != null)
        {
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / fadeDuration);
                fadeCanvas.alpha = Mathf.Lerp(startAlpha, 1f, k);
                yield return null;
            }

            fadeCanvas.alpha = 1f;
        }

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("CutsceneSkipManager: sceneToLoad is empty.");
        }
    }

    void HideUi()
    {
        if (skipText != null)
            skipText.gameObject.SetActive(false);

        if (skipCircleImage != null)
        {
            skipCircleImage.gameObject.SetActive(false);
            skipCircleImage.fillAmount = 0f;
        }
    }

    void ShowText()
    {
        if (skipText != null)
            skipText.gameObject.SetActive(true);

        if (skipCircleImage != null)
        {
            skipCircleImage.gameObject.SetActive(false);
            skipCircleImage.fillAmount = 0f;
        }
    }

    void ShowCircle()
    {
        if (skipText != null)
            skipText.gameObject.SetActive(false);

        if (skipCircleImage != null)
            skipCircleImage.gameObject.SetActive(true);
    }

    // this runs when the cutscene scene is unloaded
    void OnDestroy()
    {
        // if we left the scene *without* skipping, mark as watched
        if (!SkippedThisRun)
        {
            PlayerPrefs.SetInt(cutsceneWatchedKey, 1);
            PlayerPrefs.Save();
        }
    }
}
