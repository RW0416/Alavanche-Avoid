using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIWobble : MonoBehaviour
{
    public float xAmplitude = 8f;   // side to side amount (pixels)
    public float yAmplitude = 5f;   // up/down amount (pixels)
    public float xSpeed = 1.2f;     // how fast it moves sideways
    public float ySpeed = 1.7f;     // how fast it moves up/down

    RectTransform rect;
    Vector2 startPos;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        startPos = rect.anchoredPosition;
    }

    void Update()
    {
        float x = Mathf.Sin(Time.time * xSpeed) * xAmplitude;
        float y = Mathf.Cos(Time.time * ySpeed) * yAmplitude;

        rect.anchoredPosition = startPos + new Vector2(x, y);
    }
}
