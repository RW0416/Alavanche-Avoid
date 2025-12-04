using UnityEngine;

public class PlayerSoundEffect : MonoBehaviour
{
    [Header("核心组件")]
    public Rigidbody playerRb;
    public Transform playerBoard; 

    [Header("滑雪音效")]
    public AudioSource skiSource; 
    public float maxSkiSpeed = 30f;
    public float minSkiPitch = 0.8f;
    public float maxSkiPitch = 1.2f;

    [Header("风声特效")]
    public AudioSource windSource; 
    public float maxWindSpeed = 40f;

    [Header("地面检测")]
    public LayerMask groundLayer;
    public float groundCheckDist = 0.8f; 

    void Update()
    {
        float currentSpeed = playerRb.linearVelocity.magnitude;

        bool isGrounded = Physics.Raycast(playerBoard.position + Vector3.up * 0.1f, Vector3.down, groundCheckDist, groundLayer);

        if (isGrounded)
        {
            float speedPercent = Mathf.Clamp01(currentSpeed / maxSkiSpeed);

            skiSource.volume = Mathf.Lerp(skiSource.volume, speedPercent, Time.deltaTime * 10f);
            skiSource.pitch = Mathf.Lerp(minSkiPitch, maxSkiPitch, speedPercent);
        }
        else
        {
            skiSource.volume = Mathf.Lerp(skiSource.volume, 0f, Time.deltaTime * 20f);
        }

        float windPercent = Mathf.Clamp01(currentSpeed / maxWindSpeed);
        windSource.volume = Mathf.Lerp(windSource.volume, windPercent, Time.deltaTime * 5f);
    }

    void OnDrawGizmos()
    {
        if (playerBoard != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(playerBoard.position + Vector3.up * 0.1f, playerBoard.position + Vector3.up * 0.1f + Vector3.down * groundCheckDist);
        }
    }
}