using UnityEngine;

public class TrackSlope : MonoBehaviour
{
    [Range(0f, 45f)]
    public float slopeDegrees = 15f;

    // true = 沿着 -Z 方向向下（玩家往 -Z 滚）
    // false = 沿着 +Z 方向向下
    public bool downhillAlongNegativeZ = true;

    void Awake()
    {
        float angle = downhillAlongNegativeZ ? -slopeDegrees : slopeDegrees;
        // 绕 X 轴旋转，使 Z 方向形成坡度
        transform.rotation = Quaternion.Euler(angle, 0f, 0f);
    }
}
