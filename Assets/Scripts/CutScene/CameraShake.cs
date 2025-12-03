using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraShake : MonoBehaviour
{
    [Header("shake settings")]
    public float defaultDuration = 0.5f;
    public float defaultAmplitude = 0.3f;
    public float frequency = 20f;

    [Header("auto")]
    public bool autoShakeOnEnable = false;
    public float autoShakeDelay = 0f;

    CutsceneCameraFollow follow;
    Vector3 baseOffset = Vector3.zero;
    Coroutine shakeRoutine;

    void Awake()
    {
        follow = GetComponent<CutsceneCameraFollow>();
        if (follow != null)
            baseOffset = follow.positionOffset;
    }

    void OnEnable()
    {
        if (autoShakeOnEnable)
        {
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            shakeRoutine = StartCoroutine(ShakeRoutine(defaultDuration, defaultAmplitude, autoShakeDelay));
        }
    }

    void OnDisable()
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);

        if (follow != null)
            follow.positionOffset = baseOffset;
        else
            transform.localPosition = Vector3.zero;
    }

    public void Shake()
    {
        Shake(defaultDuration, defaultAmplitude);
    }

    public void Shake(float duration, float amplitude)
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine(duration, amplitude, 0f));
    }

    IEnumerator ShakeRoutine(float duration, float amplitude, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float damper = 1f - Mathf.Clamp01(elapsed / duration);

            float x = (Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(1f, Time.time * frequency) - 0.5f) * 2f;

            Vector3 offset = new Vector3(x, y, 0f) * amplitude * damper;

            if (follow != null)
                follow.positionOffset = baseOffset + offset;
            else
                transform.localPosition = offset;

            yield return null;
        }

        if (follow != null)
            follow.positionOffset = baseOffset;
        else
            transform.localPosition = Vector3.zero;

        shakeRoutine = null;
    }
}
