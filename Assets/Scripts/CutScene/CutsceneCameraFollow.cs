using UnityEngine;

public class CutsceneCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 lookOffset = new Vector3(0f, 1.7f, 0f);
    public float rotateSpeed = 5f;

    // this is where CameraShake writes its offset
    [HideInInspector] public Vector3 positionOffset = Vector3.zero;

    Vector3 fixedPosition;

    void Start()
    {
        fixedPosition = transform.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // base position + shake offset
        transform.position = fixedPosition + positionOffset;

        Vector3 lookPos = target.position + lookOffset;
        Vector3 dir = lookPos - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotateSpeed * Time.deltaTime
        );
    }
}
