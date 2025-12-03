using UnityEngine;

public class CylinderLocalRotator : MonoBehaviour
{
    public float rotateSpeed = 30f;  // 每秒多少度

    void Update()
    {
        Vector3 local = transform.localEulerAngles;
        local.x += rotateSpeed * Time.deltaTime;   // 这里改的是“本地 X”
        transform.localEulerAngles = local;
    }
}
