using UnityEngine;

public class FollowBall : MonoBehaviour
{
    [Header("����Ŀ�꣨������������")]
    public Transform target;

    [Header("���������λ��ƫ��")]
    public Vector3 offset = new Vector3(0f, 5f, 10f);

    [Header("����ƽ���ٶ�")]
    public float followSpeed = 5f;

    [Header("ת��ƽ���ٶ�")]
    public float lookSpeed = 10f;

    void LateUpdate()
    {
        if (!target) return;

        Vector3 desiredPos = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            followSpeed * Time.deltaTime
        );

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
