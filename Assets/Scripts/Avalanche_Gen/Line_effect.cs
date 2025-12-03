using UnityEngine;

public class LineEffectSpawner : MonoBehaviour
{
    [Header("特效设置")]
    public GameObject effectPrefab;      // 你的组合特效 prefab（下面挂了多个 ParticleSystem）
    public float spawnInterval = 0.05f;  // 生成间隔，越小越“高频”
    public float effectLifeTime = 5f;    // 如果 prefab 本身不会自动销毁，可以设置一个生命期

    [Header("线段端点")]
    public Transform pointA;             // 线段起点
    public Transform pointB;             // 线段终点

    [Header("实例设置")]
    [Tooltip("是否将生成的特效作为本物体的子物体（例如挂在 AvalancheRoot 或某个 SpawnLine 下）")]
    public bool parentToThis = true;

    private float timer = 0f;

    void Update()
    {
        if (!effectPrefab || !pointA || !pointB)
            return;

        timer += Time.deltaTime;
        while (timer >= spawnInterval)
        {
            timer -= spawnInterval;
            SpawnOne();
        }
    }

    void SpawnOne()
    {
        // 在 [A,B] 之间随机一个插值 t
        float t = Random.Range(0f, 1f);
        Vector3 pos = Vector3.Lerp(pointA.position, pointB.position, t);

        // 保留 prefab 自身的 rotation
        Quaternion rot = effectPrefab.transform.rotation;

        Transform parent = parentToThis ? transform : null;

        GameObject go = Instantiate(effectPrefab, pos, rot, parent);

        // 如果你的 prefab 里 ParticleSystem 都设置了 Stop Action = Destroy
        // 可以不写这一行；否则可以用 effectLifeTime 自动清理
        if (effectLifeTime > 0f)
        {
            Destroy(go, effectLifeTime);
        }
    }

    // 在 Scene 视图里画出这条线，方便可视化调整
    void OnDrawGizmos()
    {
        if (pointA && pointB)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pointA.position, pointB.position);
        }
    }
}
