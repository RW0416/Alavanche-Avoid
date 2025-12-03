using UnityEngine;

public class CutsceneCameraFollow : MonoBehaviour
{
    public Transform target;          // the npc
    public Vector3 lookOffset = new Vector3(0f, 1.7f, 0f);
    public float rotateSpeed = 5f;    // how fast the camera turns to keep up

    Vector3 fixedPosition;

    void Start()
    {
        // lock in the starting position so the camera doesn't move
        fixedPosition = transform.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // keep camera position fixed
        transform.position = fixedPosition;

        // smoothly rotate to look at the npc
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
