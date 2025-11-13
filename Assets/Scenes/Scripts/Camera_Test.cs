using UnityEngine;

public class FollowBall : MonoBehaviour
{
    [Header("跟随目标（拖你的球进来）")]
    public Transform target;

    [Header("相机相对球的位置偏移")]
    public Vector3 offset = new Vector3(0f, 5f, 10f);
    // 解释：Y=5 表示在球上方 5 米，Z=10 表示在球“后面”一点

    [Header("跟随平滑速度")]
    public float followSpeed = 5f;

    [Header("转向平滑速度")]
    public float lookSpeed = 10f;

    void LateUpdate()
    {
        if (!target) return;

        // 1. 计算理想相机位置：球的位置 + 偏移
        Vector3 desiredPos = target.position + offset;

        // 2. 平滑移动到这个位置
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            followSpeed * Time.deltaTime
        );

        // 3. 让相机始终看向球（朝向平滑插值）
        Vector3 lookDir = target.position - transform.position;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRot = Quaternion.LookRotation(lookDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRot,
                lookSpeed * Time.deltaTime
            );
        }
    }
}
