using UnityEngine;

public class SnowboarderRagdoll : MonoBehaviour
{
    [Header("Main character refs")]
    public SnowboarderController controller;
    public SnowboarderTricks tricks;
    public Animator animator;
    public Rigidbody mainBody;     // rb on Character_Snowboarder_01
    public Collider mainCollider;  // main capsule / box collider
    public BoardDetachOnDeath boardDetach;

    Rigidbody[] ragdollBodies;
    Collider[] ragdollColliders;

    void Awake()
    {
        if (!controller)
            controller = GetComponentInParent<SnowboarderController>();
        if (!tricks)
            tricks = GetComponentInParent<SnowboarderTricks>();
        if (!animator)
            animator = GetComponent<Animator>();
        if (!mainBody && controller)
            mainBody = controller.Body;
        if (!mainCollider && mainBody)
            mainCollider = mainBody.GetComponent<Collider>();

        // all rb/colliders under the mesh (hips, spine, etc.)
        ragdollBodies = GetComponentsInChildren<Rigidbody>(true);
        ragdollColliders = GetComponentsInChildren<Collider>(true);

        // exclude the main controller body/collider
        ragdollBodies = System.Array.FindAll(
            ragdollBodies, rb => rb != null && rb != mainBody
        );
        ragdollColliders = System.Array.FindAll(
            ragdollColliders, c => c != null && c != mainCollider
        );

        // start in normal controlled mode
        SetRagdoll(false);
    }

    public void SetRagdoll(bool enabled)
    {
        // toggle bone rigidbodies
        foreach (var rb in ragdollBodies)
        {
            if (!rb) continue;
            rb.isKinematic = !enabled;
        }

        // toggle bone colliders
        foreach (var c in ragdollColliders)
        {
            if (!c) continue;
            c.enabled = enabled;
        }

        // main controller body/collider
        if (mainBody)
            mainBody.isKinematic = enabled;
        if (mainCollider)
            mainCollider.enabled = !enabled;

        // scripts / animator
        if (controller) controller.enabled = !enabled;
        if (tricks)     tricks.enabled     = !enabled;
        if (animator)   animator.enabled   = !enabled;

        // kick the board off when we ragdoll
        if (enabled && boardDetach != null)
            boardDetach.Detach();
    }

    public void EnableRagdoll()  => SetRagdoll(true);
    public void DisableRagdoll() => SetRagdoll(false);

    // call after enabling ragdoll to shove the body
    public void ApplyImpulse(Vector3 impulse)
    {
        if (ragdollBodies == null || ragdollBodies.Length == 0)
            return;

        // push the first ragdoll body (usually the hips)
        var rb = ragdollBodies[0];
        if (rb != null && !rb.isKinematic)
            rb.AddForce(impulse, ForceMode.VelocityChange);
    }
}
