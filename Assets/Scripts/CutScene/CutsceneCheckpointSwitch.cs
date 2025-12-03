using System.Collections;
using UnityEngine;

public class CutsceneCheckpointSwitch : MonoBehaviour
{
    [Header("npcs")]
    public CutsceneNPCMover firstNpc;      // the one we are following first
    public CutsceneNPCMover secondNpc;     // identical npc for camera 2

    [Header("cameras")]
    public Camera firstCamera;
    public Camera secondCamera;

    [Header("checkpoint")]
    public Transform checkpoint;           // where the switch should happen
    public float checkpointRadius = 0.5f;  // how close before we trigger

    [Header("fade")]
    public CanvasGroup fadeCanvas;         // full-screen black image with CanvasGroup
    public float fadeDuration = 1f;

    bool hasTriggered = false;

    void Start()
    {
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.blocksRaycasts = false;
        }

        if (firstCamera != null) firstCamera.gameObject.SetActive(true);
        if (secondCamera != null) secondCamera.gameObject.SetActive(false);
    }

    void Update()
    {
        if (hasTriggered || firstNpc == null || checkpoint == null) return;

        float dist = Vector3.Distance(firstNpc.transform.position, checkpoint.position);
        if (dist <= checkpointRadius)
        {
            StartCoroutine(SwitchRoutine());
        }
    }

    IEnumerator SwitchRoutine()
    {
        hasTriggered = true;

        // freeze both npcs in place
        FreezeNpc(firstNpc, true);
        FreezeNpc(secondNpc, true);

        // fade to black
        yield return StartCoroutine(Fade(0f, 1f));

        // switch cameras
        if (firstCamera != null) firstCamera.gameObject.SetActive(false);
        if (secondCamera != null) secondCamera.gameObject.SetActive(true);

        // start second npc cutscene and unfreeze it
        if (secondNpc != null)
        {
            secondNpc.StartCutscene();      // make sure playOnStart is false on this one
            FreezeNpc(secondNpc, false);    // let it move + animate
        }

        // (optional) keep first npc frozen forever, or unfreeze if you want it alive again:
        // FreezeNpc(firstNpc, false);

        // fade back in
        yield return StartCoroutine(Fade(1f, 0f));
    }

    IEnumerator Fade(float from, float to)
    {
        if (fadeCanvas == null || fadeDuration <= 0f)
            yield break;

        float t = 0f;
        fadeCanvas.alpha = from;
        fadeCanvas.blocksRaycasts = true;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / fadeDuration);
            fadeCanvas.alpha = Mathf.Lerp(from, to, lerp);
            yield return null;
        }

        fadeCanvas.alpha = to;

        if (to <= 0f)
            fadeCanvas.blocksRaycasts = false;
    }

    void FreezeNpc(CutsceneNPCMover npc, bool freeze)
    {
        if (npc == null) return;

        if (freeze)
        {
            npc.enabled = false;
            if (npc.animator != null)
                npc.animator.speed = 0f;
        }
        else
        {
            npc.enabled = true;
            if (npc.animator != null)
                npc.animator.speed = 1f;
        }
    }
}
