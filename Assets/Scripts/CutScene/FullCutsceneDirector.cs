using System.Collections;
using UnityEngine;

public class FullCutsceneDirector : MonoBehaviour
{
    [Header("first scene")]
    public CutsceneNPCMover firstNpc;
    public Camera firstCamera;
    public Transform checkpoint;
    public float checkpointRadius = 0.5f;

    [Header("second scene")]
    public CutsceneNPCMover secondNpc;
    public Animator secondNpcAnimator;
    public string speedParamName = "Speed";          // animator float
    public Camera secondCamera;
    public CameraShake secondCameraShake;
    public string runStateName = "Idle Walk Run Blend"; // name of the locomotion state


    [Header("fade")]
    public CanvasGroup fadeCanvas;
    public float fadeDuration = 1f;

    [Header("timing")]
    public float cameraShakeDuration = 2f;
    public float shakeAmplitude = 0.4f;
    public float turnBackDuration = 0.5f;
    public float lookBackHoldDuration = 1f;
    public float turnForwardDuration = 0.5f;

    [Header("avalanche")]
    public GameObject avalancheObject;

    bool triggered = false;
    int speedHash;
    bool speedHashValid = false;

    void Start()
    {
        // fade starts clear
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.blocksRaycasts = false;
        }

        // first cam on, second off
        if (firstCamera != null) firstCamera.gameObject.SetActive(true);
        if (secondCamera != null) secondCamera.gameObject.SetActive(false);

        if (avalancheObject != null)
            avalancheObject.SetActive(false);

        if (secondNpcAnimator == null && secondNpc != null)
            secondNpcAnimator = secondNpc.animator;

        if (!string.IsNullOrEmpty(speedParamName))
        {
            speedHash = Animator.StringToHash(speedParamName);
            speedHashValid = true;
        }

        // freeze npc2 anim at start
        if (secondNpcAnimator != null)
        {
            secondNpcAnimator.speed = 0f;
            SetSecondNpcSpeed(0f);
        }
    }

    void Update()
    {
        if (triggered || firstNpc == null || checkpoint == null)
            return;

        float dist = Vector3.Distance(firstNpc.transform.position, checkpoint.position);
        if (dist <= checkpointRadius)
        {
            StartCoroutine(MainSequence());
        }
    }

    IEnumerator MainSequence()
    {
        triggered = true;

        // ---------- end scene 1 ----------
        if (firstNpc != null) firstNpc.StopCutscene();
        if (firstNpc != null && firstNpc.animator != null)
            firstNpc.animator.speed = 0f;

        if (secondNpc != null) secondNpc.StopCutscene();
        if (secondNpcAnimator != null)
        {
            secondNpcAnimator.speed = 0f;
            SetSecondNpcSpeed(0f);
        }

        // fade to black
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // switch cameras under black
        if (firstCamera != null) firstCamera.gameObject.SetActive(false);
        if (secondCamera != null) secondCamera.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.05f);

        // fade back in
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        // ---------- scene 2 ----------
        // unfreeze npc2 animator but keep idle
        if (secondNpcAnimator != null)
        {
            secondNpcAnimator.speed = 1f;
            SetSecondNpcSpeed(0f);
        }

        // camera shake
        if (secondCameraShake != null)
            secondCameraShake.Shake(cameraShakeDuration, shakeAmplitude);

        yield return new WaitForSeconds(cameraShakeDuration);

        // turn around to look behind
        if (secondNpc != null)
            yield return StartCoroutine(RotateTransform(secondNpc.transform, 180f, turnBackDuration));

        // show avalanche
        if (avalancheObject != null)
            avalancheObject.SetActive(true);

        yield return new WaitForSeconds(lookBackHoldDuration);

        // turn back to face camera
        if (secondNpc != null)
            yield return StartCoroutine(RotateTransform(secondNpc.transform, 180f, turnForwardDuration));

        // ---------- FORCE RUN ----------
        ForceSecondNpcRun();          // shove animator into run state

        if (secondNpc != null)
            secondNpc.StartCutscene(); // this actually moves him along his waypoint
    }

    void ForceSecondNpcRun()
    {
        if (secondNpcAnimator == null) return;

        secondNpcAnimator.speed = 1f;

        // slam into the locomotion state even if params are wrong
        if (!string.IsNullOrEmpty(runStateName))
            secondNpcAnimator.CrossFade(runStateName, 0.1f, 0);

        // hard-code the usual Starter Assets parameters
        secondNpcAnimator.SetBool("Grounded", true);
        secondNpcAnimator.SetFloat("Speed", 1f);
        secondNpcAnimator.SetFloat("MotionSpeed", 1f);

        // also respect custom speed param if you changed the name
        SetSecondNpcSpeed(1f);

    }

    void SetSecondNpcSpeed(float value)
    {
        if (!speedHashValid || secondNpcAnimator == null) return;
        secondNpcAnimator.SetFloat(speedHash, value);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeCanvas == null || duration <= 0f)
            yield break;

        fadeCanvas.blocksRaycasts = true;
        float t = 0f;
        fadeCanvas.alpha = from;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            fadeCanvas.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }

        fadeCanvas.alpha = to;

        if (to <= 0f)
            fadeCanvas.blocksRaycasts = false;
    }

    IEnumerator RotateTransform(Transform target, float angleY, float duration)
    {
        if (target == null || duration <= 0f)
            yield break;

        Quaternion start = target.rotation;
        Quaternion end = start * Quaternion.Euler(0f, angleY, 0f);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            target.rotation = Quaternion.Slerp(start, end, k);
            yield return null;
        }

        target.rotation = end;
    }
}
