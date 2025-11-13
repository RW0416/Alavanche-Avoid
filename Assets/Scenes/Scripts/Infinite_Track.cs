using System.Collections.Generic;
using UnityEngine;

public class InfiniteTrackRandom : MonoBehaviour
{
    [Header("References")]
    public Transform player;               // 球
    public GameObject[] trackPrefabs;      // 多种 Track 段 prefab（至少 1 个）

    [Header("Track Settings")]
    public int segmentCount = 5;           // 同时存在多少段
    public float segmentLength = 120f;     // 每段长度（和 ProBuilder Size Z 一样）

    // 约定：TrackRoot 的本地 -Z 是“往下滑”的方向
    private readonly List<Transform> segments = new List<Transform>();

    void Start()
    {
        if (trackPrefabs == null || trackPrefabs.Length == 0)
        {
            Debug.LogError("InfiniteTrackRandom: 请在 Inspector 里填至少一个 trackPrefab！");
            enabled = false;
            return;
        }

        // 一开始先生成 segmentCount 段，排在本地 -Z 方向
        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 localPos = new Vector3(0f, 0f, -segmentLength * i);
            Transform seg = SpawnRandomSegment(localPos);
            segments.Add(seg);
        }
    }

    void Update()
    {
        if (segments.Count == 0 || player == null) return;

        // 玩家在 TrackRoot 本地坐标系中的位置
        Vector3 playerLocal = transform.InverseTransformPoint(player.position);

        // 队伍中“最靠后的那一段”（z 最大的那块，因为我们往本地 -Z 下坡）
        Transform first = segments[0];
        Vector3 firstLocal = first.localPosition;

        // 当玩家的 z 比 first 小超过一段长度，就认为这一段已经远远在后面，可以回收
        if (playerLocal.z < firstLocal.z - segmentLength * 0.5f)
        {
            RecycleFirstSegment();
        }
    }

    Transform SpawnRandomSegment(Vector3 localPos)
    {
        // 随机选一个 prefab
        GameObject prefab = trackPrefabs[Random.Range(0, trackPrefabs.Length)];
        GameObject segObj = Instantiate(prefab, transform);
        Transform seg = segObj.transform;

        seg.localPosition = localPos;
        seg.localRotation = Quaternion.identity;

        return seg;
    }

    void RecycleFirstSegment()
    {
        // 取队伍中“最新”的那一段（最前面那块）
        Transform last = segments[segments.Count - 1];

        // 计算新段的位置：在 last 的前面接一段（沿本地 -Z）
        Vector3 newLocalPos = last.localPosition;
        newLocalPos.z -= segmentLength;

        // 删除最老的那一段
        Transform first = segments[0];
        segments.RemoveAt(0);

        // 在前方生成一块随机的新段
        Transform newSeg = SpawnRandomSegment(newLocalPos);
        segments.Add(newSeg);

        Destroy(first.gameObject, 1f);
    }
}
