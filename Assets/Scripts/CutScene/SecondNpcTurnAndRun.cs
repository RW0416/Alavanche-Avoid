using System.Collections;
using UnityEngine;

public class SecondNpcTurnAndRun : MonoBehaviour
{
    [Header("references")]
    public CutsceneNPCMover mover;        // same mover script you already use
    public Animator animator;
    public string speedParamName = "Speed";

    [Header("timing")]
    public float idleBeforeLookBack = 0.4f;
    public float turnBackDuration = 0.4f;
    public float lookBackHold = 0.5f;
    public float turnForwardDuration = 0.4f;

    [Header("avalanche")]
    public GameObject avalancheObject;    // keep this disabled until cutscene
    public float avalancheDelay = 0.2f;

    bool started;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (mover == null)
            mover = GetComponent<CutsceneNPCMover>();

        // lock everything so nothing moves before scene 2
        if (mover != null)
            mover.enabled = true;       // script can be on, but it won't move until StartCutscene()
        if (animator != null)
        {
            animator.speed = 0f;        // freeze pose
            SetSpeed(0f);
        }

        if (avalancheObject != null)
            avalancheObject.SetActive(false);
    }

    public void PlaySequence()
    {
        if (started) return;
        started = true;
        StartCoroutine(SequenceRoutine());
    }

    IEnumerator SequenceRoutine()
    {
        // unfreeze animator, stay idle
        if (animator != null)
            animator.speed = 1f;

        SetSpeed(0f);
        yield return new WaitForSeconds(idleBeforeLookBack);

        // 1) turn 180 to look behind
        yield return RotateBy(180f, turnBackDuration);

        // 2) show avalanche slightly after the turn
        if (avalancheDelay > 0f)
            yield return new WaitForSeconds(avalancheDelay);

        if (avalancheObject != null)
            avalancheObject.SetActive(true);  // your avalanche prefab / controller :contentReference[oaicite:0]{index=0}

        // hold looking back
        yield return new WaitForSeconds(lookBackHold);

        // 3) turn back towards camera (another 180)
        yield return RotateBy(180f, turnForwardDuration);

        // 4) start running along the path
        if (mover != null)
            mover.StartCutscene();  // this will set Speed>0 and make him run
    }

    IEnumerator RotateBy(float angleY, float duration)
    {
        if (duration <= 0f) yield break;

        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, angleY, 0f);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            transform.rotation = Quaternion.Slerp(startRot, endRot, k);
            yield return null;
        }

        transform.rotation = endRot;
    }

    void SetSpeed(float v)
    {
        if (animator != null && !string.IsNullOrEmpty(speedParamName))
            animator.SetFloat(speedParamName, v);
    }
}
