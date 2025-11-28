using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public float lifetime = 1f;                      // how long it lives
    public float moveUpSpeed = 1.5f;                 // vertical drift speed
    public Vector3 baseOffset = new Vector3(0.8f, 2f, 0f); // position relative to player

    TMP_Text tmpText;
    Transform target;
    Vector3 currentOffset;

    public void Initialize(string text, Transform followTarget)
    {
        tmpText = GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            tmpText.text = text;
        }

        target = followTarget;
        currentOffset = baseOffset;  // start offset beside / above the player

        // initial position
        if (target != null)
        {
            transform.position = target.position + currentOffset;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // gently float upward while anchored to player
        currentOffset += Vector3.up * moveUpSpeed * dt;

        if (target != null)
        {
            transform.position = target.position + currentOffset;
        }

        // face camera
        if (Camera.main != null)
        {
            transform.forward = Camera.main.transform.forward;
        }

        lifetime -= dt;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
