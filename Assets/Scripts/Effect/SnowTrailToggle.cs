using UnityEngine;

public class SnowTrailToggle : MonoBehaviour
{
    [Header("Settings")]
    public ParticleSystem snowParticle; 
    public float groundCheckDist = 0.5f; 
    public LayerMask groundLayer; 
    public Transform detectionPoint;

    void Update()
    {
        if (snowParticle == null) return;
        bool isGrounded = Physics.Raycast(detectionPoint.position + Vector3.up * 0.1f, Vector3.down, groundCheckDist, groundLayer);

        var emission = snowParticle.emission;
        emission.enabled = isGrounded;
    }

    void OnDrawGizmos()
    {
        if (detectionPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(detectionPoint.position + Vector3.up * 0.1f, detectionPoint.position + Vector3.up * 0.1f + Vector3.down * groundCheckDist);
        }
    }
}
