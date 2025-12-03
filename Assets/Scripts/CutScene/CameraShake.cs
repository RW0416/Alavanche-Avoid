using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("shake settings")]
    public float duration = 0.5f;
    public float amplitude = 0.3f;
    public float frequency = 20f;

    [Header("auto")]
    public bool autoShakeOnEnable = false;
    public float autoShakeDelay = 0f;

    Vector3 _originalLocalPos;
    Coroutine _shakeRoutine;

    void Awake()
    {
        _originalLocalPos = transform.localPosition;
    }

    void OnEnable()
    {
        _originalLocalPos = transform.localPosition;

        if (autoShakeOnEnable)
        {
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeWithDelay(autoShakeDelay));
        }
    }

    void OnDisable()
    {
        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
        transform.localPosition = _originalLocalPos;
    }

    public void Shake()
    {
        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(ShakeRoutine(duration, amplitude));
    }

    public void Shake(float customDuration, float customAmplitude)
    {
        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(ShakeRoutine(customDuration, customAmplitude));
    }

    IEnumerator ShakeWithDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        yield return ShakeRoutine(duration, amplitude);
        _shakeRoutine = null;
    }

    IEnumerator ShakeRoutine(float dur, float amp)
    {
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;

            float damper = 1f - Mathf.Clamp01(elapsed / dur);

            float x = (Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(1f, Time.time * frequency) - 0.5f) * 2f;

            Vector3 offset = new Vector3(x, y, 0f) * amp * damper;
            transform.localPosition = _originalLocalPos + offset;

            yield return null;
        }

        transform.localPosition = _originalLocalPos;
        _shakeRoutine = null;
    }
}
