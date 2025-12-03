using System.Collections.Generic;
using UnityEngine;

public class AvalancheChunkSpawner : MonoBehaviour
{
    [Header("生成设置")]
    public GameObject chunkPrefab;
    public float spawnInterval = 0.2f;
    public float moveSpeed = 2f;
    public float lifeTime = 10f;

    [Header("随机缩放")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.4f);

    [Header("方向调节")]
    [Tooltip("控制往 -Z（雪崩前进方向）移动的强度")]
    public float zFactor = 1f;

    [Tooltip("控制往 -Y（朝下掉落）移动的强度")]
    public float yFactor = 1f;

    [Header("引用")]
    public Transform avalancheRoot;

    private readonly List<Transform> spawned = new List<Transform>();
    float timer = 0f;

    void Awake()
    {
        if (avalancheRoot == null && transform.parent != null)
            avalancheRoot = transform.parent;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer -= spawnInterval;
            SpawnOne();
        }

        MoveChunks();
    }

    void SpawnOne()
    {
        if (!chunkPrefab || !avalancheRoot) return;

        Quaternion prefabRot = chunkPrefab.transform.rotation;

        GameObject go = Instantiate(chunkPrefab, transform.position, prefabRot, avalancheRoot);

        float s = Random.Range(scaleRange.x, scaleRange.y);
        go.transform.localScale = new Vector3(s, s, s);

        spawned.Add(go.transform);
        Destroy(go, lifeTime);
    }

    void MoveChunks()
    {
        if (!avalancheRoot) return;

        //  可调节世界空间方向
        Vector3 dir =
            (-avalancheRoot.forward * zFactor +
             -avalancheRoot.up * yFactor).normalized;

        float step = moveSpeed * Time.deltaTime;

        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i] == null)
            {
                spawned.RemoveAt(i);
                continue;
            }

            spawned[i].position += dir * step;
        }
    }
}
