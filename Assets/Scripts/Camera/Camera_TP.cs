using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("目标设置")]
    public Transform target;
    public float distance = 6f;
    public float heightOffset = 1.5f;

    [Header("鼠标控制")]
    public Vector2 sensitivity = new Vector2(2f, 1.5f);
    public float minPitch = -30f;
    public float maxPitch = 70f;

    [Header("平滑 & 碰撞")]
    public float smoothTime = 0.05f;
    public LayerMask collisionMask;

    float yaw;
    float pitch;
    Vector3 camVelocity;
    Vector2 lookInput;

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("[ThirdPersonCamera] target 未设置！");
        }


        var e = transform.eulerAngles;
        yaw = 180;
        pitch = e.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        yaw += lookInput.x * sensitivity.x;
        pitch -= lookInput.y * sensitivity.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Vector3 targetCenter = target.position + Vector3.up * heightOffset;

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rot * new Vector3(0f, 0f, -distance);
        Vector3 desiredPos = targetCenter + offset;

        if (Physics.Linecast(targetCenter, desiredPos, out RaycastHit hit, collisionMask, QueryTriggerInteraction.Ignore))
        {
            desiredPos = hit.point + hit.normal * 0.2f;
        }

        Vector3 smoothedPos = Vector3.SmoothDamp(transform.position, desiredPos, ref camVelocity, smoothTime);
        transform.position = smoothedPos;

        transform.LookAt(targetCenter, Vector3.up);
    }

}
