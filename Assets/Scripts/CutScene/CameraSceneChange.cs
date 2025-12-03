using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraSceneChange : MonoBehaviour
{
    [Header("avalanch to listen for")]
    public Transform avalancheRoot;          // drag Avalanche_Root here

    [Header("scene")]
    public string sceneToLoad = "Game Scene";   // set this to your game scene name

    [Header("fade")]
    public CanvasGroup fadeCanvas;              // same black fade canvas
    public float fadeDuration = 1f;

    [Header("options")]
    public bool triggerOnce = true;

    bool triggered = false;

    void Start()
    {
        // make sure fade starts invisible
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.blocksRaycasts = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered && triggerOnce) return;
        if (avalancheRoot == null) return;

        // check if the collider belongs to the avalanche object (or any child)
        if (other.transform == avalancheRoot || other.transform.IsChildOf(avalancheRoot))
        {
            Debug.Log("CameraSceneChange: avalanche hit camera, starting fade");
            StartCoroutine(FadeAndLoad());
        }
    }

    IEnumerator FadeAndLoad()
    {
        triggered = true;

        if (fadeCanvas != null)
        {
            fadeCanvas.blocksRaycasts = true;

            float t = 0f;
            float startAlpha = fadeCanvas.alpha;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / fadeDuration);
                fadeCanvas.alpha = Mathf.Lerp(startAlpha, 1f, k);
                yield return null;
            }

            fadeCanvas.alpha = 1f;
        }

        Debug.Log("CameraSceneChange: loading scene " + sceneToLoad);
        SceneManager.LoadScene(sceneToLoad);
    }
}
