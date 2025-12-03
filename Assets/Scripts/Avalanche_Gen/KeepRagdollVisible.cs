using UnityEngine;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class KeepRagdollVisible : MonoBehaviour
{
    public float boundsSize = 500f;

    void Awake()
    {
        var smr = GetComponent<SkinnedMeshRenderer>();
        if (smr == null) return;

        smr.updateWhenOffscreen = true;

        // huge local bounds so flying ragdoll never gets culled
        smr.localBounds = new Bounds(
            Vector3.zero,
            Vector3.one * boundsSize
        );
    }
}
