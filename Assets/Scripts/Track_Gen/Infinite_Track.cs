using System.Collections.Generic;
using UnityEngine;

public class InfiniteTrackRandom : MonoBehaviour
{
    [Header("References")]
    public Transform player;        
    public GameObject[] trackPrefabs;  

    [Header("Track Settings")]
    public int segmentCount = 5;        
    public float segmentLength = 120f;    

    private readonly List<Transform> segments = new List<Transform>();

    void Start()
    {
        if (trackPrefabs == null || trackPrefabs.Length == 0)
        {
            Debug.LogError("InfiniteTrackRandom: ���� Inspector ��������һ�� trackPrefab��");
            enabled = false;
            return;
        }

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

        Vector3 playerLocal = transform.InverseTransformPoint(player.position);

        Transform first = segments[0];
        Vector3 firstLocal = first.localPosition;

        if (playerLocal.z < firstLocal.z - segmentLength * 0.5f)
        {
            RecycleFirstSegment();
        }
    }

    Transform SpawnRandomSegment(Vector3 localPos)
    {
        GameObject prefab = trackPrefabs[Random.Range(0, trackPrefabs.Length)];
        GameObject segObj = Instantiate(prefab, transform);
        Transform seg = segObj.transform;

        seg.localPosition = localPos;
        seg.localRotation = Quaternion.identity;

        return seg;
    }

    void RecycleFirstSegment()
    {
        Transform last = segments[segments.Count - 1];

        Vector3 newLocalPos = last.localPosition;
        newLocalPos.z -= segmentLength;

        Transform first = segments[0];
        segments.RemoveAt(0);

        Transform newSeg = SpawnRandomSegment(newLocalPos);
        segments.Add(newSeg);

        Destroy(first.gameObject, 15f);
    }
}
